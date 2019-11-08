using CatalogAPI.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace CatalogAPI.Infrastructure
{
        public class CatalogContext
        {
            private IConfiguration configuration;
            private IMongoDatabase database;

        public CatalogContext(IConfiguration _configuration)
        {
            this.configuration = _configuration;
            var connectionstring = configuration.GetValue<string>("MongoSettings:ConnectionString");
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(connectionstring));
            settings.SslSettings =  new SslSettings(){EnabledSslProtocols = SslProtocols.Tls12};
            MongoClient client = new MongoClient(settings);
                if(client!=null)
                {
                    this.database = client.GetDatabase(configuration.GetValue<string>("MongoSettings:Database"));
                }
            }

            public IMongoCollection<CatalogItem> Catalog
            {
                get
                {
                    return this.database.GetCollection<CatalogItem>("products");
                }
            }

        }
}
