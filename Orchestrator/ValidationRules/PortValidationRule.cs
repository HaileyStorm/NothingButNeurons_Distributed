using System.Globalization;
using System.Windows.Controls;

namespace NothingButNeurons.Orchestrator.ValidationRules;

public class PortValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {


        int port;
        if (!int.TryParse(value.ToString(), out port))
        {
            return new ValidationResult(false, "Port must be a number.");
        }

        if (port == MainWindow.ServiceMonitorPort)
        {
            return new ValidationResult(false, $"Port {MainWindow.ServiceMonitorPort} is not allowed.");
        }

        if (port < 1024 || port > 65535)
        {
            return new ValidationResult(false, $"Port must be between 1024 and 65535, and not {MainWindow.ServiceMonitorPort}.");
        }

        return ValidationResult.ValidResult;
    }
}

