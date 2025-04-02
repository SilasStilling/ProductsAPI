using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing;
using WebShopLibrary;
using WebShopLibrary.Database;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using System.Text;




namespace ProductsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private UserRepository _userRepository;
        public UsersController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // GET: api/<UsersController>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet]
        public ActionResult<User> GetAll()
        {
            var user = _userRepository.GetAll();
            if (!user.Any()) return NoContent();
            return Ok(user);
        }

        // GET api/<UsersController>/5
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}")]
        public ActionResult<User> Get(int id)
        {
            var user = _userRepository.Get(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        // POST api/<UsersController>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        public ActionResult<User> Post([FromBody] User newUser)
        {
            try
            {
                var createdUser = _userRepository.Add(newUser);
                return CreatedAtAction(nameof(Get), new { id = createdUser.Id }, createdUser);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/<UsersController>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        [HttpDelete("{id}")]
        public ActionResult<User> Delete(int id)
        {
            var user = _userRepository.Get(id); // Retrieve the user first
            if (user == null)
            {
                return NotFound();
            }
            _userRepository.Remove(id);
            return Ok(user);
        }

        // POST api/<UsersController>/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] User model, [FromServices] JwtService jwtService)
        {
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Username == model.Username);

            if (user == null || !VerifyPassword(model.Password, user.Password))
            {
                return Unauthorized();
            }

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
