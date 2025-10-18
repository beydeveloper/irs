using System;
using System.Collections.Generic;

namespace Irsafe
{
    public class AssemblyInjectionService
    {
        public class AssemblyPayload
        {
            public string AssemblyCode { get; set; } = string.Empty;
            public string ExecutionType { get; set; } = "bat";
            public string Description { get; set; } = string.Empty;
            public bool AutoExecute { get; set; } = false;
            public DateTime CreatedDate { get; set; } = DateTime.Now;
            public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

            public AssemblyPayload()
            {
                Metadata = new Dictionary<string, string>();
            }
        }

        public string SerializePayload(AssemblyPayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            var lines = new List<string>
            {
                "==ASSEMBLY_PAYLOAD_START==",
                $"ExecutionType: {payload.ExecutionType}",
                $"Description: {payload.Description}",
                $"AutoExecute: {payload.AutoExecute}",
                $"CreatedDate: {payload.CreatedDate:O}",
                "Metadata:",
            };

            foreach (var kvp in payload.Metadata)
            {
                lines.Add($"  {kvp.Key}: {kvp.Value}");
            }

            lines.Add("==ASSEMBLY_CODE_START==");
            lines.Add(payload.AssemblyCode);
            lines.Add("==ASSEMBLY_CODE_END==");
            lines.Add("==ASSEMBLY_PAYLOAD_END==");

            return string.Join(Environment.NewLine, lines);
        }

        public AssemblyPayload DeserializePayload(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            var payload = new AssemblyPayload();
            var lines = data.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
            
            bool inMetadata = false;
            bool inCode = false;
            var codeLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("==ASSEMBLY_CODE_START=="))
                {
                    inCode = true;
                    inMetadata = false;
                    continue;
                }

                if (line.StartsWith("==ASSEMBLY_CODE_END=="))
                {
                    inCode = false;
                    continue;
                }

                if (inCode)
                {
                    codeLines.Add(line);
                    continue;
                }

                if (line.StartsWith("Metadata:"))
                {
                    inMetadata = true;
                    continue;
                }

                if (inMetadata && line.StartsWith("  "))
                {
                    var parts = line.Trim().Split(new[] { ": " }, 2, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        payload.Metadata[parts[0]] = parts[1];
                    }
                    continue;
                }

                if (line.StartsWith("ExecutionType: "))
                {
                    payload.ExecutionType = line.Substring("ExecutionType: ".Length);
                }
                else if (line.StartsWith("Description: "))
                {
                    payload.Description = line.Substring("Description: ".Length);
                }
                else if (line.StartsWith("AutoExecute: "))
                {
                    payload.AutoExecute = bool.Parse(line.Substring("AutoExecute: ".Length));
                }
                else if (line.StartsWith("CreatedDate: "))
                {
                    payload.CreatedDate = DateTime.Parse(line.Substring("CreatedDate: ".Length));
                }
            }

            payload.AssemblyCode = string.Join(Environment.NewLine, codeLines);
            return payload;
        }

        public void EmbedPayloadIntoImage(AssemblyPayload payload, string sourceImagePath, string targetImagePath)
        {
            var steganographyService = new SteganographyService();
            string serializedPayload = SerializePayload(payload);
            steganographyService.EmbedTextInImage(sourceImagePath, targetImagePath, serializedPayload);
        }

        public AssemblyPayload ExtractPayloadFromImage(string imagePath)
        {
            var steganographyService = new SteganographyService();
            string extractedData = steganographyService.ExtractTextFromImage(imagePath);
            
            if (string.IsNullOrWhiteSpace(extractedData))
                throw new InvalidOperationException("No data found in image");

            return DeserializePayload(extractedData);
        }

        public bool IsValidPayload(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return false;

            return data.Contains("==ASSEMBLY_PAYLOAD_START==") && 
                   data.Contains("==ASSEMBLY_PAYLOAD_END==") &&
                   data.Contains("==ASSEMBLY_CODE_START==") &&
                   data.Contains("==ASSEMBLY_CODE_END==");
        }
    }
}
