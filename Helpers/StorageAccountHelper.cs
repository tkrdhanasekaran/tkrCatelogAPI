using CatalogAPI.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using System.Threading.Tasks;

namespace MyCatalogApi.Helpers
{
    public class StorageAccountHelper
    {
        public string storageConnectionString;
        public string tableConnectionString;
        private CloudStorageAccount storageAccount;
        private CloudStorageAccount tablestorageAccount;
        private CloudBlobClient blobClient;

        public string StorageConnectionString
        {
            get { return storageConnectionString; }
            set
            {
                this.storageConnectionString = value;
                storageAccount = CloudStorageAccount.Parse(this.storageConnectionString);
          }
        }
        public string TableConnectionString
        {
            get { return tableConnectionString; }
            set
            {
                this.tableConnectionString = value;
                tablestorageAccount = CloudStorageAccount.Parse(this.tableConnectionString);
            }
        }



        public async Task<string> UploadFileToBlobAsync(string containerName, string fileName, Stream fileStream)
        {
            blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            BlobContainerPermissions permissions = new BlobContainerPermissions()
            {
                PublicAccess = BlobContainerPublicAccessType.Container
            };
            await container.SetPermissionsAsync(permissions);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(fileName);
            await blob.DeleteIfExistsAsync();
            await blob.UploadFromStreamAsync(fileStream);
            return blob.Uri.AbsoluteUri;
        }

        public async Task<CatalogEntity> SaveToTableAsync(CatalogItem item)
        {
            CatalogEntity catalogEntity = new CatalogEntity(item.Name, item.Id)
            {
                ImageUrl = item.ImageUrl,
                ReorderLevel = item.ReorderLevel,
                Quantity = item.Quantity,
                Price = item.Price,
                ManufacturingDate = item.ManufacturingDate
            };
            //var tableClient = storageAccount.CreateCloudTableClient();
            var tableClient = tablestorageAccount.CreateCloudTableClient();
            var catalogTable = tableClient.GetTableReference("catalog");
            await catalogTable.CreateIfNotExistsAsync();
            TableOperation operation = TableOperation.InsertOrMerge(catalogEntity);
            var tableResult = await catalogTable.ExecuteAsync(operation);
            return tableResult.Result as CatalogEntity;

        }
    }
}
