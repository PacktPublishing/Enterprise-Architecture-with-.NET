using System.Text;
using System.Globalization;
using System.Net.Http.Headers;

// The DotCMIS and PortCMIS clients have so many problems (see CMISTestClient project) that it was easier
// to simply implement the required behaviours directly in HTTP client calls. It may also be helpful when
// trying to plug the EDM on the IAM, removing the default basic HTTP authorization header.
internal class EDMClient
{
    private static readonly string EDMDemoEditorFolderURL = "http://host.docker.internal:8080/alfresco/api/-default-/public/cmis/versions/1.1/browser/root/DemoEditor";

    public static async Task<string> SendDocument(string name, string specialType, Dictionary<string, string> metadata, string docFilename, byte[] docContent)
    {
        var base64EncodedAuthenticationString = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("admin:admin"));

        HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, EDMDemoEditorFolderURL);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

        var content = new MultipartFormDataContent("--------------" + DateTime.Now.ToString(CultureInfo.InvariantCulture));
        content.Add(new StreamContent(new MemoryStream(docContent)), "file", docFilename);
        content.Add(new StringContent("createDocument"), "\"cmisAction\"");
        content.Add(new StringContent("cmis:objectTypeId"), "\"propertyId[0]\"");
        content.Add(new StringContent("cmis:document"), "\"propertyValue[0]\"");
        content.Add(new StringContent("cmis:name"), "\"propertyId[1]\"");
        content.Add(new StringContent(name), "\"propertyValue[1]\"");
        content.Add(new StringContent("cmis:secondaryObjectTypeIds"), "\"propertyId[2]\"");
        content.Add(new StringContent("P:de:" + specialType), "\"propertyValue[2]\"");
        int indexContent = 3;
        foreach (KeyValuePair<string, string> entry in metadata)
        {
            content.Add(new StringContent(entry.Key), "\"propertyId[" + indexContent + "]\"");
            content.Add(new StringContent(entry.Value), "\"propertyValue[" + indexContent + "]\"");
            indexContent++;
        }

        requestMessage.Content = content;
        var response = await client.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
