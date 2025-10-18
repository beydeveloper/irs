using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;

namespace Irsafe
{
    public class AssemblyExecutorService
    {
        public class ExecutionResult
        {
            public bool Success { get; set; }
            public string Output { get; set; } = string.Empty;
            public string ErrorOutput { get; set; } = string.Empty;
            public int ExitCode { get; set; }
            public TimeSpan ExecutionTime { get; set; }
            public string ExecutionPath { get; set; } = string.Empty;
        }

        public class ExecutionOptions
        {
            public bool RunAsAdministrator { get; set; } = false;
            public bool ShowWindow { get; set; } = true;
            public int TimeoutSeconds { get; set; } = 30;
            public string WorkingDirectory { get; set; } = string.Empty;
            public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
            public bool CaptureOutput { get; set; } = true;
            public bool DeleteTempFileAfterExecution { get; set; } = true;
        }

        private readonly string _tempDirectory;

        public event EventHandler<string> OutputReceived;
        public event EventHandler<string> ErrorReceived;
        public event EventHandler<ExecutionResult> ExecutionCompleted;

        public AssemblyExecutorService()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "Ýrsafe_Assembly_Execution");
            Directory.CreateDirectory(_tempDirectory);
        }

        public async Task<ExecutionResult> ExecuteAssemblyAsync(AssemblyInjectionService.AssemblyPayload payload, ExecutionOptions options = null)
        {
            options ??= new ExecutionOptions();
            
            var result = new ExecutionResult();
            var startTime = DateTime.Now;
            
            
            try
            {
                if (string.IsNullOrWhiteSpace(payload.AssemblyCode))
                {
                    throw new ArgumentException("Assembly code is empty or null");
                }

                string tempFilePath = CreateTempExecutionFile(payload, options.WorkingDirectory);
                result.ExecutionPath = tempFilePath;

                result = await ExecuteFileAsync(tempFilePath, payload.ExecutionType, options);
                
                if (options.DeleteTempFileAfterExecution && File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        result.ErrorOutput += $"\nWarning: Could not delete temporary file: {ex.Message}";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorOutput = ex.Message;
                result.ExitCode = -1;
            }
            finally
            {
                result.ExecutionTime = DateTime.Now - startTime;
                ExecutionCompleted?.Invoke(this, result);
            }

            return result;
        }

        public async Task<ExecutionResult> ExecuteWithAnimationEffectsAsync(AssemblyInjectionService.AssemblyPayload payload, ExecutionOptions options = null)
        {
            var enhancedPayload = new AssemblyInjectionService.AssemblyPayload
            {
                AssemblyCode = WrapWithAnimationEffects(payload.AssemblyCode),
                ExecutionType = payload.ExecutionType,
                Description = $"Enhanced with animations: {payload.Description}",
                AutoExecute = payload.AutoExecute,
                Metadata = new Dictionary<string, string>(payload.Metadata)
            };

            enhancedPayload.Metadata["Enhanced"] = "true";
            enhancedPayload.Metadata["AnimationMode"] = "Windows_Modern";

            return await ExecuteAssemblyAsync(enhancedPayload, options);
        }

        public ValidationResult ValidateAssemblyCode(string assemblyCode, string executionType)
        {
            var result = new ValidationResult { IsValid = true };
            var warnings = new List<string>();
            var risks = new List<string>();

            var dangerousCommands = new[]
            {
                "format", "del ", "rd ", "rmdir", "shutdown", "restart",
                "reg delete", "net user", "net localgroup", "diskpart",
                "bcdedit", "powercfg", "sc delete", "taskkill /f"
            };

            var suspiciousPatterns = new[]
            {
                "powershell", "cmd.exe", "wscript", "cscript", "mshta",
                "rundll32", "regsvr32", "bitsadmin", "certutil"
            };

            foreach (var dangerous in dangerousCommands)
            {
                if (assemblyCode.ToLower().Contains(dangerous.ToLower()))
                {
                    risks.Add($"Potentially dangerous command detected: {dangerous}");
                    result.RiskLevel = Math.Max(result.RiskLevel, 3);
                }
            }

            foreach (var suspicious in suspiciousPatterns)
            {
                if (assemblyCode.ToLower().Contains(suspicious.ToLower()))
                {
                    warnings.Add($"Suspicious pattern detected: {suspicious}");
                    result.RiskLevel = Math.Max(result.RiskLevel, 2);
                }
            }

            result.Warnings = warnings;
            result.Risks = risks;
            result.IsValid = result.RiskLevel < 3;

            return result;
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public int RiskLevel { get; set; } = 0;
            public List<string> Warnings { get; set; } = new List<string>();
            public List<string> Risks { get; set; } = new List<string>();
        }

        private string CreateTempExecutionFile(AssemblyInjectionService.AssemblyPayload payload, string workingDirectory = "")
        {
            string extension = GetFileExtension(payload.ExecutionType);
            string fileName = $"assembly_exec_{DateTime.Now:yyyyMMdd_HHmmss_fff}{extension}";
            
            string directory = !string.IsNullOrEmpty(workingDirectory) ? workingDirectory : _tempDirectory;
            Directory.CreateDirectory(directory);
            
            string filePath = Path.Combine(directory, fileName);
            
            string codeWithMetadata = AddMetadataComments(payload);
            
            File.WriteAllText(filePath, codeWithMetadata);
            return filePath;
        }

        private async Task<ExecutionResult> ExecuteFileAsync(string filePath, string executionType, ExecutionOptions options)
        {
            var result = new ExecutionResult();
            var startInfo = new ProcessStartInfo();

            ConfigureProcessStartInfo(startInfo, filePath, executionType, options);

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = startInfo;
                    
                    if (options.CaptureOutput)
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                result.Output += e.Data + Environment.NewLine;
                                OutputReceived?.Invoke(this, e.Data);
                            }
                        };
                        
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                result.ErrorOutput += e.Data + Environment.NewLine;
                                ErrorReceived?.Invoke(this, e.Data);
                            }
                        };
                    }

                    process.Start();

                    if (options.CaptureOutput)
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }

                    bool completed = await Task.Run(() => process.WaitForExit(options.TimeoutSeconds * 1000));
                    
                    if (!completed)
                    {
                        process.Kill();
                        result.Success = false;
                        result.ErrorOutput = $"Execution timed out after {options.TimeoutSeconds} seconds";
                        result.ExitCode = -1;
                    }
                    else
                    {
                        result.ExitCode = process.ExitCode;
                        result.Success = process.ExitCode == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorOutput = ex.Message;
                result.ExitCode = -1;
            }

            return result;
        }

        private void ConfigureProcessStartInfo(ProcessStartInfo startInfo, string filePath, string executionType, ExecutionOptions options)
        {
            switch (executionType.ToLower())
            {
                case "bat":
                case "cmd":
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/c \"{filePath}\"";
                    break;
                case "ps1":
                case "powershell":
                    startInfo.FileName = "powershell.exe";
                    startInfo.Arguments = $"-ExecutionPolicy Bypass -File \"{filePath}\"";
                    break;
                case "vbs":
                    startInfo.FileName = "wscript.exe";
                    startInfo.Arguments = $"\"{filePath}\"";
                    break;
                default:
                    startInfo.FileName = filePath;
                    break;
            }

            startInfo.WorkingDirectory = !string.IsNullOrEmpty(options.WorkingDirectory) ? options.WorkingDirectory : Path.GetDirectoryName(filePath);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = !options.ShowWindow;
            startInfo.RedirectStandardOutput = options.CaptureOutput;
            startInfo.RedirectStandardError = options.CaptureOutput;

            foreach (var kvp in options.EnvironmentVariables)
            {
                startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
            }
        }

        private string GetFileExtension(string executionType)
        {
            return executionType.ToLower() switch
            {
                "bat" or "cmd" => ".bat",
                "ps1" or "powershell" => ".ps1",
                "vbs" => ".vbs",
                _ => ".bat"
            };
        }

        private string AddMetadataComments(AssemblyInjectionService.AssemblyPayload payload)
        {
            string commentChar = payload.ExecutionType.ToLower() switch
            {
                "bat" or "cmd" => "REM",
                "ps1" or "powershell" => "#",
                "vbs" => "'",
                _ => "REM"
            };

            var metadataLines = new[]
            {
                $"{commentChar} Assembly Code Generated by Ýrsafe",
                $"{commentChar} Description: {payload.Description}",
                $"{commentChar} Created: {payload.CreatedDate}",
                $"{commentChar} Execution Type: {payload.ExecutionType}",
                $"{commentChar} Auto Execute: {payload.AutoExecute}",
                ""
            };

            return string.Join(Environment.NewLine, metadataLines) + payload.AssemblyCode;
        }

        private string WrapWithAnimationEffects(string originalCode)
        {
            var animationWrapper = @"
@echo off
title Ýrsafe - Enhanced Animation System
color 0A

echo.
echo ????????????????????????????????????????????????????????????????
echo ?                Ýrsafe Animation Framework                     ?
echo ?              Enhanced Visual Experience                       ?
echo ????????????????????????????????????????????????????????????????
echo.

REM Add smooth loading animation
for /L %%i in (1,1,20) do (
    cls
    echo Loading Enhanced Experience...
    set ""bar=""
    for /L %%j in (1,1,%%i) do set ""bar=!bar!?""
    echo [!bar!                    ] %%i%%/20
    timeout /t 1 /nobreak >nul
)

cls
echo.
echo ? Starting Enhanced Assembly Execution ?
echo.

REM Original assembly code starts here
" + originalCode + @"

echo.
echo ? Assembly execution completed successfully
echo ? Ýrsafe Enhanced Animation System ?
pause
";

            return animationWrapper;
        }

        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch (Exception)
            {
            }
        }

        ~AssemblyExecutorService()
        {
            Cleanup();
        }
    }
}