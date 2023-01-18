using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

class Program
{
  static async Task Main(string[] args)
  {
    var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

    var sourceInstance = configuration.GetSection("SourceApiKey").Value;
    var targetInstance = configuration.GetSection("TargetApiKey").Value;
    var baseUrl = "https://api.onspring.com";

    var sourceRecord = 1;
    var sourceField = 12250;
    var sourceFile = 1161;
    var file = await GetFileFromInstance(sourceInstance, sourceRecord, sourceField, sourceFile);

    var targetRecord = 1;
    var targetField = 8728;
    var result = await AddFileToInstance(targetInstance, targetRecord, targetField, file);
    Console.WriteLine(result == 0 ? "Success" : "Failure");

    async Task<FileResult> GetFileFromInstance(string apikey, int recordId, int fieldId, int fileId)
    {
      var fileInfoUri = $"{baseUrl}/Files/recordId/{recordId}/fieldId/{fieldId}/fileId/{fileId}";
      var fileUri = $"{baseUrl}/Files/recordId/{recordId}/fieldId/{fieldId}/fileId/{fileId}/file";
      var fileInfoRequest = new HttpRequestMessage(HttpMethod.Get, fileInfoUri);
      var fileRequest = new HttpRequestMessage(HttpMethod.Get, fileUri);

      var httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Add("x-apikey", apikey);
      httpClient.DefaultRequestHeaders.Add("x-api-version", "2");

      var fileInfo = await httpClient.GetFromJsonAsync<FileInfo>(fileInfoUri);
      var fileRes = await httpClient.SendAsync(fileRequest);
      
      if (fileRes.IsSuccessStatusCode is false)
      {
        return null;
      }

      var fileStream = await fileRes.Content.ReadAsStreamAsync();
      return new FileResult(fileStream, fileInfo.Name, fileInfo.ContentType);
    }

    async Task<int> AddFileToInstance(string apiKey, int recordId, int fieldId, FileResult file)
    {
      if (file is not null)
      {
        return 1;
      }

      var requestUri = $"{baseUrl}/Files";
      var streamContent = new StreamContent(file.FileStream);
      streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

      var multiPartContent = new MultipartFormDataContent
        {
          { streamContent, "File", file.FileName },
          { new StringContent(recordId.ToString()), "RecordId" },
          { new StringContent(fieldId.ToString()), "FieldId" },
          { new StringContent(""), "Notes" },
        };

      var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
      {
        Content = multiPartContent,
      };

      httpRequest.Headers.Add("x-apikey", apiKey);
      httpRequest.Headers.Add("x-api-version", "2");

      var httpClient = new HttpClient();
      var httpResponse = await httpClient.SendAsync(httpRequest);
      return httpResponse.StatusCode == System.Net.HttpStatusCode.Created ? 0 : 1;
    }
  }

  class FileInfo
  {
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
  }
  
  class FileResult
  {
    public FileResult(Stream fileStream, string fileName, string contentType)
    {
      FileStream = fileStream;
      FileName = fileName;
      ContentType = contentType;
    }

    public Stream FileStream { get; set; }
    public String FileName { get; set; }
    public String ContentType { get; set; }
  }
}
