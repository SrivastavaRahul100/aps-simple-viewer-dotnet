using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Autodesk.Oss;
using Autodesk.Oss.Model;

public partial class APS
{
    private async Task EnsureBucketExists(string bucketKey)
    {        var auth = await GetInternalToken();
        var ossClient = new OssClient(_sdkManager);
        try
        {
            await ossClient.GetBucketDetailsAsync(accessToken: auth.AccessToken,bucketKey);
        }
        catch (OssApiException ex)
        {
            if (ex.HttpResponseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var bucketsPayload = new CreateBucketsPayload
                {
                    BucketKey = bucketKey,
                   PolicyKey = PolicyKey.Persistent
                };
                await ossClient.CreateBucketAsync(auth.AccessToken,Region.US, bucketsPayload);
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<ObjectDetails> UploadModel(string objectName, string pathToFile)
    {
        await EnsureBucketExists(_bucket);
        var auth = await GetInternalToken();
        var ossClient = new OssClient(_sdkManager);
        var objectDetails = await ossClient.Upload(_bucket, objectName, pathToFile, accessToken:auth.AccessToken, new System.Threading.CancellationToken());
        return objectDetails;
    }

    public async Task<IEnumerable<ObjectDetails>> GetObjects()
    {
        await EnsureBucketExists(_bucket);
        var auth = await GetInternalToken();
        var ossClient = new OssClient(_sdkManager);
        const int PageSize = 64;
        var results = new List<ObjectDetails>();
        var response = await ossClient.GetObjectsAsync(auth.AccessToken,_bucket, PageSize);
        results.AddRange(response.Items);
        while (!string.IsNullOrEmpty(response.Next))
        {
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(response.Next).Query);
            response = await ossClient.GetObjectsAsync(auth.AccessToken ,_bucket, PageSize, startAt: queryParams["startAt"]);
            results.AddRange(response.Items);
        }
        return results;
    }
}
