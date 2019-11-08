using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CatalogAPI.Infrastructure;
using CatalogAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MyCatalogApi.Helpers;
using static CatalogAPI.Models.CatalogItem;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CatalogAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private CatalogContext db;
        private IConfiguration configuration;
        public CatalogController(CatalogContext _db, IConfiguration configuration)
        {
            this.db = _db;
            this.configuration = configuration;
        }

        [HttpGet("", Name = "GetProducts")]
        [AllowAnonymous]
        public async Task<ActionResult<List<CatalogItem>>> GetProductsAsync()
        {
            var result = await db.Catalog.FindAsync<CatalogItem>(FilterDefinition<CatalogItem>.Empty);
            return result.ToList();
        }

        [Authorize(Roles = "admin")]
        [HttpPost("", Name = "AddProduct")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<CatalogItem> AddProduct(CatalogItem item)
        {
            //explicitly validating the model
            TryValidateModel(item);
            if (ModelState.IsValid)
            {
                this.db.Catalog.InsertOne(item);
                return Created("", item);  //200
            }
            else
            {
                return BadRequest(ModelState); //400
            }
        }

        [HttpGet("{id}", Name = "FindbyId")]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult<CatalogItem>> FindProductbyId(string id)
        {
            var builder = Builders<CatalogItem>.Filter;
            var filter = builder.Eq("Id", id);
            var result = await db.Catalog.FindAsync(filter);
            var item = result.FirstOrDefault();
            if (item == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(item);
            }
        }
        [HttpPost("product")]
        public async Task<ActionResult<CatalogItem>> AddProductAsync()
        {
            // var imagename = SaveImageLocal(Request.Form.Files[0]);
            try
            {
                var image = Request.Form.Files[0];
                if (image == null || image.Length < 0 || !ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                string fileName = await SaveImageToCloudAsync(image);
                var catalogItem = new CatalogItem()
                {
                    Name = Request.Form["name"],
                    Price = Double.Parse(Request.Form["Price"]),
                    Quantity = Int32.Parse(Request.Form["quantity"]),
                    ReorderLevel = Int32.Parse(Request.Form["reorderLevel"]),
                    ManufacturingDate = DateTime.Parse(Request.Form["manufacturingDate"]),
                    Vendors = new List<Vendor>(),
                    ImageUrl = fileName

                };
                db.Catalog.InsertOne(catalogItem);//save to mongodb
                BackToTableAsync(catalogItem).GetAwaiter().GetResult();

                return catalogItem;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        [NonAction]
        private string SaveImageLocal(IFormFile image)
        {

            //var imagename = $"{Guid.NewGuid()}_{Request.Form.Files[0].FileName}";
            var imagename = $"{Guid.NewGuid()}_{image.FileName}";
            var dirName = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            var filePath = Path.Combine(dirName, imagename);
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(fs);
            }
            return $"/Images/{imagename}";
        }
        private async Task<string> SaveImageToCloudAsync(IFormFile image)
        {
            var imageName = $"{Guid.NewGuid()}_{image.FileName}";
            StorageAccountHelper storageHelper = new StorageAccountHelper();
            storageHelper.StorageConnectionString = configuration.GetConnectionString("StorageConnection");
            var fileUrl = await storageHelper.UploadFileToBlobAsync("eshopimages", imageName, image.OpenReadStream());
            return fileUrl;
        }
        [NonAction]
        async Task<CatalogEntity> BackToTableAsync(CatalogItem item)
        {
            StorageAccountHelper storageHelper = new StorageAccountHelper();
            //storageHelper.StorageConnectionString = configuration.GetConnectionString("StorageConnection");
            storageHelper.tableConnectionString = configuration.GetConnectionString("TableConnection");
            return await storageHelper.SaveToTableAsync(item);
        }
    }
}
