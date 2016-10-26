namespace csp_violation_recorder
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using Funq;
    using Serilog;
    using ServiceStack;
    using ServiceStack.Text;

    public class Program
    {
        static void Main(string[] args)
        {
            var serviceUrl = "http://127.0.0.1:8855/";
            new AppHost(serviceUrl).Init().Start("http://*:8855/");
            $"ServiceStack SelfHost listening at {serviceUrl} ".Print();
            Process.Start(serviceUrl);
            Console.ReadLine();
        }
    }

    public class AppHost : AppSelfHostBase
    {
        private readonly string serviceUrl;

        public AppHost(string serviceUrl) : base("cspRecorder", typeof(RecorderService).Assembly)
        {
            this.serviceUrl = serviceUrl;
        }

        public override void Configure(Container container)
        {
            const string contentType = "application/csp-report";

            ContentTypes.Register(contentType, (req, o, stream) => JsonSerializer.SerializeToStream(o.GetType(), stream),
                JsonSerializer.DeserializeFromStream);
            
            Log.Logger = CreateLogger();
        }

        private ILogger CreateLogger()
            => new LoggerConfiguration()
                .Enrich.WithProperty("AppName", "CSP Violation")
                .WriteTo.Seq("http://127.0.0.1:5341/").CreateLogger();
    }

    public class RecorderService : Service
    {
        private static readonly ILogger Logger = Log.ForContext<RecorderService>();

        public void Post(CspViolation violation)
        {
            Logger.ForContext("violation", violation.Report, true)
                  .Information($"CSP-Violation received: {violation.Report.ViolatedDirective}");
        }
    }

    [DataContract]
    [Route("/violation")]
    public class CspViolation : IReturnVoid, IPost
    {
        [DataMember(Name = "csp-report")]
        public CspReport Report { get; set; }
    }
    
    [DataContract]
    public class CspReport
    {
        [DataMember(Name = "document-uri")]
        public Uri DocumentUri { get; set; }

        [DataMember(Name = "referrer")]
        public Uri Referrer { get; set; }

        [DataMember(Name = "violated-directive")]
        public string ViolatedDirective { get; set; }

        [DataMember(Name = "original-policy")]
        public string OriginalPolicy { get; set; }

        [DataMember(Name = "blocked-uri")]
        public Uri BlockedUri { get; set; }

        [DataMember(Name = "script-sample")]
        public string ScriptSample { get; set; }

        [DataMember(Name = "line-number")]
        public int LineNumber { get; set; }
    }
}
