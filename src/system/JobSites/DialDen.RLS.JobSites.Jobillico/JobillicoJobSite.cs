using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DialDen.RLS.JobSites.Jobillico;

internal sealed class JobillicoJobSite(IHttpClientFactory clients, JobillicoOptions options) : IJobSite
{
    public string Id => "jobillico";

    public string Name => "Jobillico";

    public async Task Publish(JobSitePosting posting, CancellationToken cancellationToken)
    {
        var request = new XElement("request",
            new XElement("bsandboxoff", options.UseProduction ? "1" : "0"),
            new XElement("oauth",
                new XElement("sparam", options.Username),
                new XElement("sparam", options.Password)),
            new XElement("ocreator", new XElement("sparam", options.Username)),
            new XElement("job",
                new XElement("elang", "en"),
                new XElement("irecruiter", options.RecruiterId),
                new XElement("stitle_en", posting.Title),
                new XElement("srefnumber", posting.PostingId.ToString(CultureInfo.InvariantCulture)),
                new XElement("olocations", new XElement("ilocation", options.LocationId)),
                new XElement("sdescription_en", posting.Description)));
        using var content = new StringContent(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + request.ToString(SaveOptions.DisableFormatting),
            Encoding.UTF8, "application/xml");
        using var client = clients.CreateClient("Jobillico");
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(options.Endpoint, content, cancellationToken);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Jobillico did not respond within the HTTP timeout.", exception);
        }
        using (response)
        {
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1_000_000
            });
            var results = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
            if (results.Descendants("error").Any())
            {
                throw new InvalidOperationException("Jobillico rejected the posting.");
            }
            var jobs = results.Root?.Name == "results" ? results.Root.Elements("job").ToArray() : [];
            if (jobs.Length != 1 || !long.TryParse(jobs[0].Element("ijob")?.Value,
                    NumberStyles.None, CultureInfo.InvariantCulture, out var jobId) || jobId <= 0)
            {
                throw new InvalidOperationException("Jobillico returned no valid job identifier.");
            }
        }
    }
}
