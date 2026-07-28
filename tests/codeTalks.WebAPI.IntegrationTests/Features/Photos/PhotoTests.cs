using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudinaryDotNet.Actions;
using codeTalks.Application.Features.Channels.Commands.JoinChannel;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Models;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Services.FileStorage;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace codeTalks.WebAPI.IntegrationTests.Features.Photos;

/// <summary>
/// Full-pipeline coverage of the multipart photo endpoints — <c>PUT/DELETE /api/users/profile-photo</c>
/// and <c>PUT/DELETE /api/channels/{id}/thumbnail-photo</c>. Cloudinary is faked (the shared
/// <see cref="ICloudinaryService"/> singleton), so uploads return a canned secure URL and Postgres is
/// the source of truth for the persisted <c>ProfilePhotoURL</c>/<c>ThumbnailPhotoURL</c>. All of these
/// endpoints carry <c>[Authorize]</c>, so a missing token is rejected at the auth gate → <b>401</b>;
/// an authenticated non-owner acting on a channel thumbnail is a handler-level authorization failure
/// → <b>403</b> (<c>AuthorizationException</c>). Replacing an existing photo deletes the old asset
/// first, which we assert against the fake.
/// </summary>
public sealed class PhotoTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    // ---- profile photo (PUT/DELETE /api/users/profile-photo) -----------------

    [Fact]
    public async Task Update_profile_photo_without_token_returns_401()
    {
        // profile-photo carries [Authorize], so the auth gate rejects before the handler runs.
        var response = await Client.PutAsync("/api/users/profile-photo", ImageForm());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_profile_photo_persists_url_and_returns_path()
    {
        var (user, client) = await CreateUserAsync();
        const string uploadedUrl = "https://res.cloudinary.com/demo/image/upload/v1700000000/profile/alpha.png";
        StubUpload(uploadedUrl);

        var response = await client.PutAsync("/api/users/profile-photo", ImageForm());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<UpdatedProfilePhotoDto>(JsonWebOptions))!;
        dto.NewProfilePhotoPath.Should().Be(uploadedUrl);

        (await ProfilePhotoUrlAsync(user.UserId)).Should().Be(uploadedUrl);
    }

    [Fact]
    public async Task Update_profile_photo_with_invalid_content_type_returns_400()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.PutAsync("/api/users/profile-photo", ImageForm(contentType: "text/plain"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_profile_photo_replaces_and_deletes_the_previous_asset()
    {
        var (user, client) = await CreateUserAsync();

        StubUpload("https://res.cloudinary.com/demo/image/upload/v1700000000/profile/first.png");
        (await client.PutAsync("/api/users/profile-photo", ImageForm())).EnsureSuccessStatusCode();

        // Second upload should delete the first asset (public id derived from the stored URL) first.
        Cloudinary.ClearReceivedCalls();
        const string secondUrl = "https://res.cloudinary.com/demo/image/upload/v1700000001/profile/second.png";
        StubUpload(secondUrl);

        (await client.PutAsync("/api/users/profile-photo", ImageForm())).EnsureSuccessStatusCode();

        (await ProfilePhotoUrlAsync(user.UserId)).Should().Be(secondUrl);
        await Cloudinary.Received(1).DeleteImageAsync("profile/first", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_profile_photo_clears_the_stored_url()
    {
        var (user, client) = await CreateUserAsync();
        StubUpload("https://res.cloudinary.com/demo/image/upload/v1700000000/profile/alpha.png");
        (await client.PutAsync("/api/users/profile-photo", ImageForm())).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync("/api/users/profile-photo");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ProfilePhotoUrlAsync(user.UserId)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_profile_photo_when_none_uploaded_returns_400()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.DeleteAsync("/api/users/profile-photo");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- channel thumbnail (PUT/DELETE /api/channels/{id}/thumbnail-photo) ----

    [Fact]
    public async Task Update_thumbnail_without_token_returns_401()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);

        // thumbnail-photo carries [Authorize], so the auth gate rejects before the handler runs.
        var response = await Client.PutAsync($"/api/channels/{channel.Id}/thumbnail-photo", ImageForm());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_thumbnail_as_owner_persists_url_and_returns_path()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);
        const string uploadedUrl = "https://res.cloudinary.com/demo/image/upload/v1700000000/thumb/alpha.png";
        StubUpload(uploadedUrl);

        var response = await client.PutAsync($"/api/channels/{channel.Id}/thumbnail-photo", ImageForm());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<UpdatedThumbnailPhotoDto>(JsonWebOptions))!;
        dto.NewThumbnailPhotoPath.Should().Be(uploadedUrl);

        (await ThumbnailPhotoUrlAsync(channel.Id)).Should().Be(uploadedUrl);
    }

    [Fact]
    public async Task Uploaded_thumbnail_is_exposed_by_the_channel_read_endpoints()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        const string uploadedUrl = "https://res.cloudinary.com/demo/image/upload/v1700000000/thumb/read.png";
        StubUpload(uploadedUrl);
        (await ownerClient.PutAsync($"/api/channels/{channel.Id}/thumbnail-photo", ImageForm())).EnsureSuccessStatusCode();

        // GetById — the member's view of a channel they belong to.
        var getById = await ownerClient.GetAsync($"/api/channels/{channel.Id}");
        var byId = (await getById.Content.ReadFromJsonAsync<ChannelByIdDto>(JsonWebOptions))!;
        byId.ThumbnailPhotoURL.Should().Be(uploadedUrl);

        // GetAll — the discovery list, which only shows channels the caller has *not* joined,
        // so ask as an outsider and isolate the row with the unique channel name.
        var (_, outsiderClient) = await CreateUserAsync();
        var list = await outsiderClient.GetAsync(
            $"/api/channels?page=1&pageSize=50&title={Uri.EscapeDataString(channel.Name)}");
        var page = (await list.Content.ReadFromJsonAsync<ChannelsByUserIdListModel>(JsonWebOptions))!;
        page.Items.Should().ContainSingle(c => c.Id == channel.Id)
            .Which.ThumbnailPhotoURL.Should().Be(uploadedUrl);
    }

    [Fact]
    public async Task Update_thumbnail_by_non_owner_member_returns_403()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        var (_, memberClient) = await JoinAsync(channel);
        StubUpload("https://res.cloudinary.com/demo/image/upload/v1700000000/thumb/x.png");

        var response = await memberClient.PutAsync($"/api/channels/{channel.Id}/thumbnail-photo", ImageForm());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_thumbnail_on_unknown_channel_returns_404()
    {
        var (_, client) = await CreateUserAsync();
        StubUpload("https://res.cloudinary.com/demo/image/upload/v1700000000/thumb/x.png");

        var response = await client.PutAsync(
            $"/api/channels/{Guid.NewGuid():N}/thumbnail-photo", ImageForm());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_thumbnail_with_invalid_content_type_returns_400()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var response = await client.PutAsync(
            $"/api/channels/{channel.Id}/thumbnail-photo", ImageForm(contentType: "text/plain"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_thumbnail_as_owner_clears_the_stored_url()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);
        StubUpload("https://res.cloudinary.com/demo/image/upload/v1700000000/thumb/alpha.png");
        (await client.PutAsync($"/api/channels/{channel.Id}/thumbnail-photo", ImageForm())).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/channels/{channel.Id}/thumbnail-photo");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ThumbnailPhotoUrlAsync(channel.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_thumbnail_when_none_uploaded_returns_400()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var response = await client.DeleteAsync($"/api/channels/{channel.Id}/thumbnail-photo");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_thumbnail_by_non_owner_member_returns_403()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        var (_, memberClient) = await JoinAsync(channel);

        var response = await memberClient.DeleteAsync($"/api/channels/{channel.Id}/thumbnail-photo");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- helpers -------------------------------------------------------------

    /// <summary>The shared, faked Cloudinary service (registered as a singleton by the factory).</summary>
    private ICloudinaryService Cloudinary => Factory.Services.GetRequiredService<ICloudinaryService>();

    /// <summary>Makes the fake upload return a canned secure URL for any file.</summary>
    private void StubUpload(string secureUrl) =>
        Cloudinary.UploadImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>())
            .Returns(new ImageUploadResult { SecureUrl = new Uri(secureUrl) });

    /// <summary>A multipart body with a single "image" file part, matching the action parameter name.</summary>
    private static MultipartFormDataContent ImageForm(string contentType = "image/png", int byteCount = 8)
    {
        var file = new ByteArrayContent(Enumerable.Repeat((byte)0x42, byteCount).ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent { { file, "image", "photo.png" } };
        return form;
    }

    private async Task<(AuthenticatedUser User, HttpClient Client)> JoinAsync(ChannelInfo channel)
    {
        var (user, client) = await CreateUserAsync();
        var join = await client.PostAsJsonAsync("/api/channels/join", new JoinChannelCommand
        {
            InviteCode = channel.InviteCode
        });
        join.EnsureSuccessStatusCode();
        return (user, client);
    }

    private async Task<string?> ProfilePhotoUrlAsync(string userId)
    {
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.Set<User>().AsNoTracking().SingleAsync(u => u.Id == userId)).ProfilePhotoURL;
    }

    private async Task<string?> ThumbnailPhotoUrlAsync(string channelId)
    {
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.Set<Channel>().AsNoTracking().SingleAsync(c => c.Id == channelId)).ThumbnailPhotoURL;
    }
}