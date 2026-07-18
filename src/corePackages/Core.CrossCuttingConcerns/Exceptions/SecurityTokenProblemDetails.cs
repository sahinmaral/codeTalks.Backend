using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Core.CrossCuttingConcerns.Exceptions;

public class SecurityTokenProblemDetails : ProblemDetails
{
    public override string ToString() => JsonConvert.SerializeObject(this, new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    });
}