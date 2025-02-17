using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing;
using WebShopLibrary;
using WebShopLibrary.Database;




namespace ProductsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private ProductRepositoryDb _productRepository;
        public ProductsController(ProductRepositoryDb productRepository)
        {
            _productRepository = productRepository;
        }
        // GET: api/<ProductsController>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetAll()
        {
            var product = _productRepository.GetAll();
            if (!product.Any()) return NoContent();
            return Ok(product);
        }

        // GET api/<ProductsController>/5
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}")]
        public ActionResult<Product> GetById(int id)
        {
            var product = _productRepository.Get(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        // POST api/<ProductsController>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        public ActionResult<Product> Post([FromBody] Product newProduct)
        {
            try
            {
                var createdProduct = _productRepository.Add(newProduct);
                return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE api/<ProductsController>/5
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete("{id}")]
        public ActionResult<Product> Delete(int id)
        {
            var product = _productRepository.Get(id); // Retrieve the product first
            if (product == null)
            {
                return NotFound();
            }
            _productRepository.Remove(id); // Perform the removal
            return Ok(product);
        }
    }
}
