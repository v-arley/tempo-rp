
using System;
using System.Linq;

using TmIde.ViewModels.Help;

using Xunit;

namespace TmIde.Tests.ViewModels;

public sealed class LanguageReferenceViewModelTests
{
    [Fact]
    public void Constructor_CreatesNineSections()
    {
        var viewModel = new LanguageReferenceViewModel();

        Assert.Equal(9, viewModel.Sections.Count);
    }

    [Fact]
    public void Constructor_SelectsFirstSection()
    {
        var viewModel = new LanguageReferenceViewModel();

        Assert.Same(
            viewModel.Sections[0],
            viewModel.SelectedSection);

        Assert.Equal(
            "Palabras reservadas",
            viewModel.SelectedSection?.Title);
    }

    [Fact]
    public void Sections_HaveExpectedOrder()
    {
        var viewModel = new LanguageReferenceViewModel();

        string[] expected =
        [
            "Palabras reservadas",
            "Sintaxis",
            "Estructuras de control",
            "Funciones",
            "Operaciones",
            "Semántica",
            "Tipos de datos",
            "Importaciones",
            "Ejemplos"
        ];

        var actual = viewModel.Sections
            .Select(section => section.Title)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Sections_HaveUniqueTitles()
    {
        var viewModel = new LanguageReferenceViewModel();

        var titles = viewModel.Sections
            .Select(section => section.Title)
            .ToArray();

        Assert.Equal(
            titles.Length,
            titles.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Sections_HaveCompleteContent()
    {
        var viewModel = new LanguageReferenceViewModel();

        Assert.All(viewModel.Sections, section =>
        {
            Assert.False(string.IsNullOrWhiteSpace(section.Title));
            Assert.False(string.IsNullOrWhiteSpace(section.Summary));
            Assert.False(string.IsNullOrWhiteSpace(section.Details));
            Assert.False(string.IsNullOrWhiteSpace(section.Example));
            Assert.False(string.IsNullOrWhiteSpace(section.Note));
        });
    }

    [Fact]
    public void SelectingSection_UpdatesSelectedSection()
    {
        var viewModel = new LanguageReferenceViewModel();

        var functions = viewModel.Sections
            .Single(section => section.Title == "Funciones");

        viewModel.SelectedSection = functions;

        Assert.Same(functions, viewModel.SelectedSection);
    }

    [Fact]
    public void FunctionsSection_ContainsComposeExample()
    {
        var viewModel = new LanguageReferenceViewModel();

        var functions = viewModel.Sections
            .Single(section => section.Title == "Funciones");

        Assert.Contains(
            "compose",
            functions.Example,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ImportsSection_ContainsImportExample()
    {
        var viewModel = new LanguageReferenceViewModel();

        var imports = viewModel.Sections
            .Single(section => section.Title == "Importaciones");

        Assert.Contains(
            "import",
            imports.Example,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ExamplesSection_ContainsExecutableSession()
    {
        var viewModel = new LanguageReferenceViewModel();

        var examples = viewModel.Sections
            .Single(section => section.Title == "Ejemplos");

        Assert.Contains(
            "session >",
            examples.Example,
            StringComparison.Ordinal);

        Assert.Contains(
            "output >>",
            examples.Example,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DataTypesSection_ContainsTempoTypes()
    {
        var viewModel = new LanguageReferenceViewModel();

        var types = viewModel.Sections
            .Single(section => section.Title == "Tipos de datos");

        Assert.Contains("sample", types.Details);
        Assert.Contains("wave", types.Details);
        Assert.Contains("tone", types.Details);
        Assert.Contains("phrase", types.Details);
        Assert.Contains("signal", types.Details);
        Assert.Contains("sequence", types.Details);
        Assert.Contains("score", types.Details);
    }
}
