# 6 · HTTP Client Factory y resiliencia con Polly

Crear instancias de HttpClient con 'new HttpClient()' es uno de los bugs más clásicos de .NET. Agota sockets y causa fallos intermitentes. La solución es IHttpClientFactory.

## 6.1 Patrón correcto

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>builder.Services.AddHttpClient&lt;StripeClient&gt;(client =&gt;</p>
<p>{</p>
<p>client.BaseAddress = new Uri("https://api.stripe.com/");</p>
<p>client.Timeout = TimeSpan.FromSeconds(30);</p>
<p>});</p>
<p>public class StripeClient</p>
<p>{</p>
<p>private readonly HttpClient _http;</p>
<p>public StripeClient(HttpClient http) =&gt; _http = http;</p>
<p>public async Task&lt;Customer&gt; CreateCustomerAsync(string email, CancellationToken ct)</p>
<p>{</p>
<p>var response = await _http.PostAsJsonAsync("v1/customers", new { email }, ct);</p>
<p>response.EnsureSuccessStatusCode();</p>
<p>return await response.Content.ReadFromJsonAsync&lt;Customer&gt;(cancellationToken: ct);</p>
<p>}</p>
<p>}</p></td>
</tr>
</tbody>
</table>

## 6.2 Polly para retry, circuit breaker y timeout

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>builder.Services</p>
<p>.AddHttpClient&lt;StripeClient&gt;(client =&gt; {</p>
<p>client.BaseAddress = new Uri("https://api.stripe.com/");</p>
<p>})</p>
<p>.AddStandardResilienceHandler(options =&gt;</p>
<p>{</p>
<p>options.Retry.MaxRetryAttempts = 3;</p>
<p>options.Retry.BackoffType = DelayBackoffType.Exponential;</p>
<p>options.Retry.UseJitter = true;</p>
<p>options.CircuitBreaker.FailureRatio = 0.5;</p>
<p>options.CircuitBreaker.MinimumThroughput = 10;</p>
<p>options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);</p>
<p>options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);</p>
<p>options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);</p>
<p>options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);</p>
<p>});</p></td>
</tr>
</tbody>
</table>



---

*Rogelio Arriaga Gonzalez*
