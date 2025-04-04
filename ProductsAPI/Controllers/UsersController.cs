using Microsoft.AspNetCore.Mvc;
using WebShopLibrary;
using WebShopLibrary.Database;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace ProductsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private static Dictionary<string, (int Attempts, DateTime? LockoutTime)> _loginAttempts = new();
        private const int MaxAttempts = 5;
        private const int LockoutMinutes = 5;

        private UserRepository _userRepository;
        public UsersController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // GET: api/<UsersController>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        //[Authorize(Roles = "admin")]
        [HttpGet]
        public ActionResult<IEnumerable<User>> GetAll()
        {
            var users = _userRepository.GetAll();
            if (!users.Any()) return NoContent();
            return Ok(users);
        }

        // GET api/<UsersController>/5
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public ActionResult<User> Get(int id)
        {
            var user = _userRepository.Get(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // POST api/<UsersController>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        [HttpPost]
        public ActionResult<User> Register([FromBody] User newUser)
        {
            if (string.IsNullOrWhiteSpace(newUser.Username) || string.IsNullOrWhiteSpace(newUser.Password))
                return BadRequest("Username and password are required.");

            var createdUser = _userRepository.Add(newUser);
            return CreatedAtAction(nameof(Get), new { id = createdUser.Id }, createdUser);
        }

        // PUT api/<UsersController>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "admin")]
        [HttpPut]
        public ActionResult<User> Put([FromBody] User user)
        {
            try
            {
                var updatedUser = _userRepository.Update(user);
                return Ok(updatedUser);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE api/<UsersController>/5
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public ActionResult<User> Delete(int id)
        {
            var user = _userRepository.Get(id);
            if (user == null) return NotFound();
            _userRepository.Remove(id);
            return Ok(user);
        }

        // POST api/<UsersController>/login
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] User model, [FromServices] JwtService jwtService)
        {
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Username == model.Username);

            if (!_loginAttempts.ContainsKey(model.Username))
                _loginAttempts[model.Username] = (0, null);

            var (attempts, lockoutTime) = _loginAttempts[model.Username];

            if (lockoutTime.HasValue && lockoutTime > DateTime.UtcNow)
            {
                return StatusCode(429, new { message = $"For mange forsøg! Prøv igen om {(lockoutTime.Value - DateTime.UtcNow).Minutes} minutter." });
            }

            if (user == null || !VerifyPassword(model.Password, user.Password))
            {
                attempts++;

                if (attempts >= MaxAttempts)
                {
                    _loginAttempts[model.Username] = (attempts, DateTime.UtcNow.AddMinutes(LockoutMinutes));
                    return StatusCode(429, new { message = $"For mange mislykkede forsøg! Kontoen er låst i {LockoutMinutes} minutter." });
                }

                _loginAttempts[model.Username] = (attempts, null);
                return Unauthorized(new { message = "Forkert brugernavn eller kodeord." });
            }

            _loginAttempts[model.Username] = (0, null);

            var token = jwtService.GenerateToken(user);
            return Ok(new
            {
                token,
                role = user.Role
            });
        }

        // PUT api/<UsersController>/change-password
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPut("change-password")]
        public ActionResult ChangePassword([FromBody] ChangePasswordModel model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("User is not authenticated.");
            }

            var user = _userRepository.GetAll().FirstOrDefault(u => u.Username == username);
            if (user == null || string.IsNullOrEmpty(model.OldPassword) || !VerifyPassword(model.OldPassword, user.Password))
            {
                return BadRequest("Old password is incorrect.");
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return BadRequest("New password and confirm password do not match.");
            }

            user.Password = HashPassword(model.NewPassword);
            _userRepository.Update(user);

            return Ok("Password changed successfully.");
        }

        private bool VerifyPassword(string enteredPassword, string storedPassword)
        {
            try
            {
                var storedPasswordBytes = Convert.FromBase64String(storedPassword);
                var salt = new byte[16];
                Buffer.BlockCopy(storedPasswordBytes, 0, salt, 0, salt.Length);

                using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(enteredPassword)))
                {
                    argon2.Salt = salt;
                    argon2.DegreeOfParallelism = 8;
                    argon2.MemorySize = 65536;
                    argon2.Iterations = 4;

                    var hash = argon2.GetBytes(32);
                    for (int i = 0; i < hash.Length; i++)
                    {
                        if (hash[i] != storedPasswordBytes[salt.Length + i])
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string HashPassword(string password)
        {
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = 8;
                argon2.MemorySize = 65536;
                argon2.Iterations = 4;

                var hash = argon2.GetBytes(32);
                var hashBytes = new byte[48];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
                Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}