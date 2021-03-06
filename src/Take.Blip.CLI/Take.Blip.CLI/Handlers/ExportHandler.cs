﻿using ITGlobal.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Take.BlipCLI.Models;
using Take.BlipCLI.Services.Interfaces;
using Take.BlipCLI.Services.Settings;

namespace Take.BlipCLI.Handlers
{
    public class ExportHandler : HandlerAsync
    {
        public const string BUILDER_BUCKET_KEY = "blip_portal:builder_working_flow";

        public INamedParameter<string> Node { get; set; }
        public INamedParameter<string> Authorization { get; set; }
        public INamedParameter<string> OutputFilePath { get; set; }
        public INamedParameter<ExportModel> Model { get; set; }
        public INamedParameter<string> Excel { get; set; }
        public IBlipClientFactory BlipClientFactory { get; set; }
        public IExcelGeneratorService ExcelGeneratorService { get; set; }

        private readonly ISettingsFile _settingsFile;
        private readonly IExportServiceFactory _exportServiceFactory;

        public ExportHandler(
            IBlipClientFactory blipClientFactory,
            IExcelGeneratorService excelGeneratorService,
            IExportServiceFactory exportServiceFactory)
        {
            _settingsFile = new SettingsFile();
            BlipClientFactory = blipClientFactory;
            ExcelGeneratorService = excelGeneratorService;
            _exportServiceFactory = exportServiceFactory;
        }


        public override async Task<int> RunAsync(string[] args)
        {
            if (!Node.IsSet && !Authorization.IsSet)
                throw new ArgumentNullException("You must provide the target bot (node) for this action. Use '-n' [--node] (or '-a' [--authorization]) parameters");

            if (!OutputFilePath.IsSet)
                throw new ArgumentNullException("You must provide the target output path for this action. Use '-o' [--output] parameter");

            var authorization = GetAuthorization();
            var outputPath = OutputFilePath.Value;
            string excel = null;

            if (Excel.IsSet && !string.IsNullOrEmpty(Excel.Value))
                excel = Excel.Value;

            switch (Model.Value)
            {
                case ExportModel.NLPModel:
                    var nlpExportInstance = _exportServiceFactory.GetNLPExportInstance();
                    await nlpExportInstance.ExportNLPModelAsync(authorization, outputPath, excel: excel);
                    return 0;
                case ExportModel.Builder:
                    var bucketExportInstance = _exportServiceFactory.GetBucketExportInstance();
                    await bucketExportInstance.ExportContentByKeyAsync(authorization, BUILDER_BUCKET_KEY, outputPath);
                    return 0;
                default:
                    break;
            }

            return -1;
        }

        public ExportModel CustomParser(string type)
        {
            var getType = TryGetContentType(type);
            if (getType.HasValue)
            {
                return getType.Value;
            }
            throw new CommandLineParameterException($"\"{type}\" was an invalid Exportable Model");
        }

        protected string GetAuthorization()
        {
            string authorization = Authorization.Value;
            if (Node.IsSet)
            {
                authorization = _settingsFile.GetNodeCredentials(Lime.Protocol.Node.Parse(Node.Value)).Authorization;
            }
            return authorization;
        }

        private ExportModel? TryGetContentType(string content)
        {
            var validContents = Enum.GetNames(typeof(ExportModel));
            var validContent = validContents.FirstOrDefault(c => c.ToLowerInvariant().Equals(content.ToLowerInvariant()));

            if (validContent != null)
                return Enum.Parse<ExportModel>(validContent);

            return null;
        }

    }

}
