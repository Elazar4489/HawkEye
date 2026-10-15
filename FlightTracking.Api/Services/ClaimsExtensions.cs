using System.Security.Claims;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Services
{
    public static class ClaimsExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirst("sub")?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        public static bool IsAdmin(this ClaimsPrincipal principal) =>
            principal.IsInRole(UserRoles.Admin);

        // A user may act on their own resources; an admin may act on any user's resources.
        public static bool CanAccessUser(this ClaimsPrincipal principal, int userId) =>
            principal.IsAdmin() || principal.GetUserId() == userId;
    }
}
