using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace codeTalks.Presentation.Filters;

/// <summary>
/// Relaxes Kestrel's minimum request-body data rate for the action it is applied to.
/// <para>
/// Kestrel defaults to aborting a request whose body arrives slower than 240 bytes/second
/// after a 5-second grace period, answering with a 400 whose message leaks the internal
/// <c>MinRequestBodyDataRate</c> detail. That floor is fine for JSON payloads, but the photo
/// endpoints receive multi-megabyte multipart bodies from phones on cellular links, where a
/// stall of a few seconds mid-upload is routine rather than an attack.
/// </para>
/// <para>
/// The rate is lowered rather than removed: a genuinely dead connection is still cut, so a
/// client cannot hold a connection open indefinitely by drip-feeding bytes. Pair this with a
/// <see cref="Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute"/> so the relaxed rate can
/// never apply to an unbounded body.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowSlowUploadsAttribute : Attribute, IResourceFilter
{
    private const double BytesPerSecond = 100;
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(30);

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        // Resource filters run before model binding, which is the last point where changing
        // the rate still matters -- binding the IFormFile parameter is what reads the body.
        var dataRateFeature = context.HttpContext.Features.Get<IHttpMinRequestBodyDataRateFeature>();

        // Absent whenever the host is not Kestrel (notably TestServer, which the integration
        // suite runs on): no data rate exists there, so there is nothing to relax.
        if (dataRateFeature is null)
            return;

        dataRateFeature.MinDataRate = new MinDataRate(BytesPerSecond, GracePeriod);
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
