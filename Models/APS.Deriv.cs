using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.ModelDerivative;
using Autodesk.ModelDerivative.Model;

public record TranslationStatus(string Status, string Progress, IEnumerable<string>? Messages);

public partial class APS
{
    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes).TrimEnd('=');
    }

    public async Task<Job> TranslateModel(string objectId, string rootFilename)
    {
        var auth = await GetInternalToken();
        var modelDerivativeClient = new ModelDerivativeClient(_sdkManager);
        var payload = new JobPayload
        {
            Input = new JobPayloadInput
            {
                Urn = Base64Encode(objectId)
            },
            Output = new JobPayloadOutput
            {
                Formats = new List<IJobPayloadFormat>
                {
                    new JobPayloadFormatSVF2
                    {
                        Views = new List<View>
                        {
                            View._2d,
                            View._3d
                        }
                    }
                }
            }
        };
        if (!string.IsNullOrEmpty(rootFilename))
        {
            payload.Input.RootFilename = rootFilename;
            payload.Input.CompressedUrn = true;
        }
        var job = await modelDerivativeClient.StartJobAsync(jobPayload: payload, accessToken: auth.AccessToken);
        return job;
    }

    public async Task<TranslationStatus> GetTranslationStatus(string urn)
    {
        var auth = await GetInternalToken();
        var modelDerivativeClient = new ModelDerivativeClient(_sdkManager);
        try
        {
            var manifest = await modelDerivativeClient.GetManifestAsync( accessToken: auth.AccessToken,urn);
            var messages = new List<string>();
            // TODO: collect messages from manifest
            return new TranslationStatus(manifest.Status, manifest.Progress, messages);
        }
        catch (ModelDerivativeApiException ex)
        {
            if (ex.HttpResponseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new TranslationStatus("n/a", "", null);
            }
            else
            {
                throw;
            }
        }
    }
}
