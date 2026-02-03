using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProjectManager.Services
{
    public class CosmosDbService<T> where T : class
    {
        private readonly Microsoft.Azure.Cosmos.Container _container;

        public CosmosDbService(CosmosClient client, string databaseName, string containerName)
        {
            _container = client.GetContainer(databaseName, containerName);
        }

        public async Task<T?> GetItemAsync(string id, string partitionKey)
        {
            try
            {
                ItemResponse<T> response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<T>> GetItemsAsync(string queryString)
        {
            var query = _container.GetItemQueryIterator<T>(new QueryDefinition(queryString));
            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }

        public async Task<(IEnumerable<T> Items, string? ContinuationToken)> GetItemsPagedAsync(string queryString, int pageSize, string? continuationToken = null)
        {
            QueryRequestOptions options = new QueryRequestOptions { MaxItemCount = pageSize };
            FeedIterator<T> iterator = _container.GetItemQueryIterator<T>(new QueryDefinition(queryString), continuationToken, options);
            if (iterator.HasMoreResults)
            {
                FeedResponse<T> response = await iterator.ReadNextAsync();
                return (response.ToList(), response.ContinuationToken);
            }
            return (Enumerable.Empty<T>(), null);
        }

        public async Task<T> AddItemAsync(T item, string partitionKey)
        {
            ItemResponse<T> response = await _container.CreateItemAsync(item, new PartitionKey(partitionKey));
            return response.Resource;
        }

        public async Task<T> UpdateItemAsync(string id, T item, string partitionKey)
        {
            ItemResponse<T> response = await _container.UpsertItemAsync(item, new PartitionKey(partitionKey));
            return response.Resource;
        }

        public async Task DeleteItemAsync(string id, string partitionKey)
        {
            await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
        }
    }
}
