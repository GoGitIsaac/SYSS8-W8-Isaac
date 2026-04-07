namespace ShoppingCartAppIntegration.Tests;

using System.Net.Http;
using System.Text.Json;
using System.Net;
using System.Text;

[TestClass]
public class Product
{
    private static readonly HttpClient client = new HttpClient();
    private static string? adminToken;
    private static int? productId;
    private static string? productName;

    private string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private async Task LoginAsAdmin()
    {
        var loginResponse = await client.PostAsync($"{GlobalContext.appUrl}/login", new StringContent(
            JsonSerializer.Serialize(new { username = "admin", password = "admin" }),
            Encoding.UTF8,
            "application/json"
        ));
        var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync()).RootElement;
        adminToken = loginJson.GetProperty("access_token").GetString();
    }

    [TestMethod]
    public async Task AdminAddsProductToTheCatalog()
    {
        // Arrange
        await LoginAsAdmin();
        productName = "product_" + RandomString(8);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, $"{GlobalContext.appUrl}/product");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { name = productName }),
            Encoding.UTF8,
            "application/json"
        );
        var response = await client.SendAsync(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.AreEqual(productName, json.GetProperty("name").GetString());
        productId = json.GetProperty("id").GetInt32();
        Assert.IsTrue(productId > 0);
    }

    [TestMethod]
    public async Task AdminRemovesProductFromTheCatalog()
    {
        // Arrange
        await LoginAsAdmin();
        productName = "product_" + RandomString(8);

        // First create a product to delete
        var createRequest = new HttpRequestMessage(HttpMethod.Post, $"{GlobalContext.appUrl}/product");
        createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        createRequest.Content = new StringContent(
            JsonSerializer.Serialize(new { name = productName }),
            Encoding.UTF8,
            "application/json"
        );
        var createResponse = await client.SendAsync(createRequest);
        int localProductId = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        // Use local variable instead of static
        int localProductId = json.GetProperty("id").GetInt32();

        // Act (Delete the product
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"{GlobalContext.appUrl}/product/{localProductId}");
        deleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        var deleteResponse = await client.SendAsync(deleteRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, deleteResponse.StatusCode);
        var deleteJson = JsonDocument.Parse(await deleteResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.AreEqual(productName, deleteJson.GetProperty("name").GetString());
        Assert.AreEqual(localProductId, deleteJson.GetProperty("id").GetInt32());
    }
}