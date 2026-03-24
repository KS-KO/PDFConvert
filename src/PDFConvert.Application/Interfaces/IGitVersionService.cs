using PDFConvert.Domain.ValueObjects;

namespace PDFConvert.Application.Interfaces;

public interface IGitVersionService
{
    GitVersionInfo GetVersionInfo();
}
