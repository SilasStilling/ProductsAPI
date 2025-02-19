using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing;
using WebShopLibrary;
using WebShopLibrary.Database;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;



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
    }
}
