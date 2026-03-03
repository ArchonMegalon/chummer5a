using Chummer.Contracts.Api;

namespace Chummer.Application.Tools;

public interface IDataExportService
{
    DataExportBundle BuildBundle(string xml);
}
