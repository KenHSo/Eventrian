namespace Eventrian.Shared.Dtos.Auth
{
    public class LoginResponseDto
    {
        public bool Success { get; set; } = false;
        public string? Token { get; set; }
        public string? Email { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Creates a successful login response with the provided email, token, and optional message.
        /// </summary>
        /// <param name="email">The authenticated user's email.</param>
        /// <param name="token">The JWT token issued.</param>
        /// <param name="message">An optional success message. Defaults to "Operation successful."</param>
        /// <returns>A <see cref="LoginResponseDto"/> indicating success.</returns>
        public static LoginResponseDto SuccessResponse(string email, string token, string message = "Operation successful.")
        {
            return new LoginResponseDto
            {
                Success = true,
                Email = email,
                Token = token,
                Message = message,
                Errors = null
            };
        }

        /// <summary>
        /// Creates a failure login response with the provided error message and optional error details.
        /// </summary>
        /// <param name="message">General failure message.</param>
        /// <param name="errors">Optional detailed error messages.</param>
        /// <returns>A <see cref="LoginResponseDto"/> indicating failure.</returns>
        public static LoginResponseDto FailureResponse(string message, List<string>? errors = null)
        {
            return new LoginResponseDto
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }
}
