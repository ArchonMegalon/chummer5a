#nullable enable annotations

using System.Security.Claims;
using Chummer.Api.Owners;
using Chummer.Contracts.Owners;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public sealed class RequestOwnerContextAccessorTests
{
    [TestMethod]
    public void Current_defaults_to_local_single_user_when_no_http_context_exists()
    {
        RequestOwnerContextAccessor accessor = new(new HttpContextAccessor());

        Assert.AreEqual(OwnerScope.LocalSingleUser.NormalizedValue, accessor.Current.NormalizedValue);
    }

    [TestMethod]
    public void Current_uses_authenticated_nameidentifier_claim_when_present()
    {
        DefaultHttpContext context = new();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "Alice@example.com")
        ], "test"));

        RequestOwnerContextAccessor accessor = new(new HttpContextAccessor
        {
            HttpContext = context
        });

        Assert.AreEqual("alice@example.com", accessor.Current.NormalizedValue);
    }

    [TestMethod]
    public void Current_uses_forwarded_header_when_enabled_and_user_is_anonymous()
    {
        DefaultHttpContext context = new();
        context.Request.Headers["X-Chummer-Owner"] = "Bob@example.com";

        RequestOwnerContextAccessor accessor = new(
            new HttpContextAccessor
            {
                HttpContext = context
            },
            headerName: "X-Chummer-Owner");

        Assert.AreEqual("bob@example.com", accessor.Current.NormalizedValue);
    }

    [TestMethod]
    public void Current_prefers_authenticated_user_over_forwarded_header()
    {
        DefaultHttpContext context = new();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "carol@example.com")
        ], "test"));
        context.Request.Headers["X-Chummer-Owner"] = "ignored@example.com";

        RequestOwnerContextAccessor accessor = new(
            new HttpContextAccessor
            {
                HttpContext = context
            },
            headerName: "X-Chummer-Owner");

        Assert.AreEqual("carol@example.com", accessor.Current.NormalizedValue);
    }
}
