using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http;
using System.Text.Json.Serialization;
using Azure;
using Azure.Storage.Blobs;
using Epical_Case.Models;
namespace Epical_Case
{
	public class ProcessPostsFunction
	{
		private readonly ILogger _logger;
		private readonly HttpClient _httpClient = new();
		private readonly BlobServiceClient _blobServiceClient;

		public ProcessPostsFunction(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
		{
			_logger = loggerFactory.CreateLogger<ProcessPostsFunction>();
			_blobServiceClient = blobServiceClient;
		}

		[Function("ProcessPostsFunction")]
		public async Task Run([TimerTrigger("*/30 * * * * *")] TimerInfo myTimer)
		{
			_logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

			var posts = await FetchPostsAsync();

			var filteredPosts = FilterPostsByUserId(posts, 1);

			await SavePostsToBlobStorageAsync(filteredPosts);

			if (myTimer.ScheduleStatus is not null)
			{
				_logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
			}
		}

		private async Task<List<Post>> FetchPostsAsync()
		{
			try
			{
				var response = await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/posts");
				response.EnsureSuccessStatusCode();
				var responseBody = await response.Content.ReadAsStringAsync();
				var posts = JsonSerializer.Deserialize<List<Post>>(responseBody);
				return posts ?? new List<Post>();
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError($"Error fetching posts: {ex.Message}");
				return new List<Post>();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Unexpected error: {ex.Message}");
				return new List<Post>();
			}
		}

		private List<Post> FilterPostsByUserId(List<Post> posts, int userId)
		{
			return posts.FindAll(post => post.UserId == userId);
		}

		private async Task SavePostsToBlobStorageAsync(List<Post> posts)
		{
			try
			{
				var containerClient = _blobServiceClient.GetBlobContainerClient("filtered-posts");
				await containerClient.CreateIfNotExistsAsync();

				var blobClient = containerClient.GetBlobClient($"filtered-posts-{DateTime.Now:yyyyMMddHHmmss}.json");
				var json = JsonSerializer.Serialize(posts);
				using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
				await blobClient.UploadAsync(stream, overwrite: true);
				_logger.LogInformation("Filtered posts saved to Blob Storage.");
			}
			catch (RequestFailedException ex)
			{
				_logger.LogError($"Error Saving posts to Blob Storage: {ex.Message}");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Unexpected error: {ex.Message}");
			}
		}
	}
}
