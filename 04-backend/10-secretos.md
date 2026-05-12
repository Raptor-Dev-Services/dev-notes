# 2 · Secretos en producción

En desarrollo local, User Secrets resuelve. En producción, los secretos viven en gestores especializados, NUNCA en archivos de configuración, repositorios, ni variables de entorno expuestas.

## 2.1 Reglas absolutas

- Cero secretos en el repositorio. Nunca, bajo ninguna circunstancia.

- Cero secretos en el Dockerfile. Una imagen es pública dentro del registry; sus capas son inspeccionables.

- Cero secretos en logs. Los strings sensibles se enmascaran al loggear.

- Rotación periódica. Cada secreto tiene fecha de caducidad — al menos cada 90 días.

- Acceso mínimo. Cada servicio usa un secreto distinto con permisos mínimos. No reutilizar.

- Auditoría. Cada acceso a un secreto queda registrado y es auditable.

## 2.2 Gestores de secretos por nube

| **Plataforma** | **Servicio** | **Cuándo usarlo** |
|----|----|----|
| **Azure** | Azure Key Vault | Stack Microsoft. Integración nativa con .NET. |
| **AWS** | AWS Secrets Manager | Stack AWS. Soporta rotación automática para RDS. |
| **AWS** | AWS SSM Parameter Store | Alternativa más barata para configuración menos sensible. |
| **GCP** | Google Secret Manager | Stack Google Cloud. |
| **Multi-cloud** | HashiCorp Vault | Cuando se quiere ser independiente del cloud provider. |
| **Self-hosted** | Bitwarden Secrets / Infisical | Para SaaS pequeños o on-premise. |

## 2.3 Integración Azure Key Vault con .NET

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C# Program.cs</em></td>
</tr>
<tr>
<td><p>using Azure.Identity;</p>
<p>var builder = WebApplication.CreateBuilder(args);</p>
<p>if (!builder.Environment.IsDevelopment())</p>
<p>{</p>
<p>var keyVaultUri = builder.Configuration["KeyVault:Uri"]</p>
<p>?? throw new InvalidOperationException("KeyVault:Uri no configurado");</p>
<p>builder.Configuration.AddAzureKeyVault(</p>
<p>new Uri(keyVaultUri),</p>
<p>new DefaultAzureCredential());</p>
<p>}</p></td>
</tr>
</tbody>
</table>

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><p><strong>Verifica que tus logs no filtran</strong></p>
<p>Después de cada cambio en logging, revisa Seq buscando palabras clave: 'password', 'secret', 'token', 'apikey', 'pk_test', 'sk_test', 'eyJ' (inicio típico de JWT). Si aparecen, hay un bug que debes corregir antes de desplegar.</p></td>
</tr>
</tbody>
</table>



> Fuente: *Building Secure and Reliable Systems* (Heather Adkins et al.) — Ch.8 Design for Security

---

*Rogelio Arriaga Gonzalez*
