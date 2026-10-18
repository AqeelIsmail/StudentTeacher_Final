using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Authentication_API.Tests
{
    public class AuthenticationService : IAuthenticationService
    {
        public bool SignInCalled { get; private set; }
        public bool SignOutCalled { get; private set; }

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties)
        {
            SignInCalled = true;

            return Task.CompletedTask;
        }

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            SignOutCalled = true;

            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync(
            HttpContext context,
            string? scheme)
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }
    }
}
