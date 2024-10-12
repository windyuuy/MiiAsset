

// Based on https://www.owasp.org/index.php/Certificate_and_Public_Key_Pinning#.Net
namespace Framework.MiiAsset.Runtime.CertificateHandlers
{
	public class AcceptAllCertificate : UnityEngine.Networking.CertificateHandler
	{

		// Encoded RSAPublicKey
		// private static string PUB_KEY = "MIID+jCCAuKgAwIBAgIRAN4Uv3cHslCgUwkt7XxsD8UwDQYJKoZIhvcNAQELBQAwgZsxCzAJBgNVBAYTAkNOMRAwDgYDVQQIDAdCZWlqaW5nMRAwDgYDVQQHDAdCZWlqaW5nMTUwMwYDVQQKDCxCZWlqaW5nIEVuZHBvaW50IE5ldHdvcmsgVGVjaG5vbG9neSBDby4sIEx0ZDEWMBQGA1UECwwNRW5kcG9pbnQgVGVhbTEZMBcGA1UEAwwQRW5kcG9pbnQgUm9vdCBDQTAeFw0yMjA5MTMwMDAwMDBaFw0yMzA5MjcyMzU5NTlaMHgxCzAJBgNVBAYTAkNOMRIwEAYDVQQIDAnljJfkuqzluIIxOTA3BgNVBAoMMOe9keaYk+aciemBk+S/oeaBr+aKgOacr++8iOWMl+S6rO+8ieaciemZkOWFrOWPuDEaMBgGA1UEAwwRKi5tZXRhLnlvdWRhby5jb20wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDdE9rbzkHmPxuzvKQ9Ge4+55jj3zPFZ38xZkl5QUKo8swHGA6TbKysZeA4pN6G9s3UBxKEMVZKoNZpv+tmQxgmAquSfcb9uGF2zz/s1/0qYBnnLkYqFJb/aQ+hPC0u7Z9dr2PdwNWuokgoVzt79unCuyVpIhEL+AON/My8w3kGJLIumlKz8giyucJ2R2R1zL9mhEmIaXSgCku6B4Xh9G28oV7rnFNqt7O5ceHRclpQIo5u75+q8SHHZooOEJTa+L+MntY7zaCvuleIoYtlEoZIf+Axhe2BaF5kxU6C/2SME/l8uZvbgDoITmDwizjCN/vA3ZobZce8qDAcgn6p+x27AgMBAAGjWzBZMC0GA1UdEQQmMCSCESoubWV0YS55b3VkYW8uY29tgg9tZXRhLnlvdWRhby5jb20wCQYDVR0TBAIwADAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDQYJKoZIhvcNAQELBQADggEBAIjDxTqeL0fYULfW826FGIRNDZf8W7QIb9I2XLwP6GGP4e8q21btUJP/s3OeGPCZt/HwBCls34zkPodiPoDDhJ0PcJGSjNPXbZtvYIJCvHJ0IzvQUjT64gLhRabhUTloL4fgphBgiAR1R1WO46xD0Yzn/US7QKKYI1Txv9FcOErTfrQKftmMgpX9v2Jfbk+IDDyp86jBvNdPv8L7O7QcLW7hR1BCBvDz913atxn+YL6ygXA/WJLo09XeaHumq1zXbNX+JmdbH1uvHGmRsMG9hX3S+zH41YADXHz31sLqhxiPcPclUr9TPoOiDxL07wTa5J0zellfaFZ4EJVjN/2SE2o=";
		protected override bool ValidateCertificate(byte[] certificateData)
		{
			return true;
			// X509Certificate2 certificate = new X509Certificate2(certificateData);
			// string pk = certificate.GetPublicKeyString();
			// if (pk.ToLower().Equals(PUB_KEY.ToLower()))
			// {
			// 	return true;
			// }
			// return false;
		}
	}
}
