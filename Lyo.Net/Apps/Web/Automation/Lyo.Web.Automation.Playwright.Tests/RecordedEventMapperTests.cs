using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Playwright.Record;

namespace Lyo.Web.Automation.Playwright.Tests;

public sealed class RecordedEventMapperTests
{
    [Fact]
    public void RecordedEventMapper_ClickWithTestId_EmitsFindAndActClick()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Click, "save", ElementLocator.CssSelector("[data-testid=\"save\"]")));

        var step = Assert.IsType<FindAndActAutomationStep>(Assert.Single(mapper.Build().Steps));
        Assert.Equal("el1", step.RefName);
        Assert.Equal(ElementLocatorKind.CssSelector, step.Locator.Kind);
        Assert.Equal("[data-testid=\"save\"]", step.Locator.Value);
        Assert.IsType<ClickElementAction>(step.Action);
    }

    [Fact]
    public void RecordedEventMapper_TypingThenBlur_EmitsOneInputText()
    {
        var mapper = new RecordedEventMapper();
        var locator = ElementLocator.Id("searchInput");
        mapper.Apply(new(RecordedDomEventKind.Click, "searchInput", locator));
        mapper.Apply(new(RecordedDomEventKind.Input, "searchInput", locator, Text: "c"));
        mapper.Apply(new(RecordedDomEventKind.Input, "searchInput", locator, Text: "co"));
        mapper.Apply(new(RecordedDomEventKind.Input, "searchInput", locator, Text: "comics"));
        mapper.Apply(new(RecordedDomEventKind.Blur, "searchInput", locator));

        var step = Assert.IsType<FindAndActAutomationStep>(Assert.Single(mapper.Build().Steps));
        var input = Assert.IsType<InputTextElementAction>(step.Action);
        Assert.Equal("comics", input.Text);
        Assert.True(input.ClearFirst);
        Assert.Equal(ElementLocatorKind.Id, step.Locator.Kind);
        Assert.Equal("searchInput", step.Locator.Value);
    }

    [Fact]
    public void RecordedEventMapper_BlurBetweenKeystrokes_KeepsOneInputText()
    {
        var mapper = new RecordedEventMapper();
        var locator = ElementLocator.Id("search_form_input");
        mapper.Apply(new(RecordedDomEventKind.Click, "search_form_input", locator));
        mapper.Apply(new(RecordedDomEventKind.Input, "search_form_input", locator, Text: "c"));
        mapper.Apply(new(RecordedDomEventKind.Blur, "search_form_input", locator));
        mapper.Apply(new(RecordedDomEventKind.Input, "search_form_input", locator, Text: "co"));
        mapper.Apply(new(RecordedDomEventKind.Blur, "search_form_input", locator));
        mapper.Apply(new(RecordedDomEventKind.Input, "search_form_input", locator, Text: "comics"));
        mapper.Apply(new(RecordedDomEventKind.Blur, "search_form_input", locator));

        var shown = Assert.IsType<InputTextElementAction>(Assert.IsType<FindAndActAutomationStep>(Assert.Single(mapper.Preview().Steps)).Action);
        Assert.Equal("comics", shown.Text);
        var saved = Assert.IsType<InputTextElementAction>(Assert.IsType<FindAndActAutomationStep>(Assert.Single(mapper.Build().Steps)).Action);
        Assert.Equal("comics", saved.Text);
        Assert.True(saved.ClearFirst);
    }

    [Fact]
    public void RecordedEventMapper_SelectChange_EmitsSelectByValue()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Select, "planNativeSel", ElementLocator.Id("planNativeSel"), Value: "b"));

        var step = Assert.IsType<FindAndActAutomationStep>(Assert.Single(mapper.Build().Steps));
        var select = Assert.IsType<SelectByValueElementAction>(step.Action);
        Assert.Equal("b", select.Value);
        Assert.Equal("planNativeSel", step.Locator.Value);
    }

    [Fact]
    public void RecordedEventMapper_ClickNavigation_OmitsExtraNavigate()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Click, "next", ElementLocator.Id("nextPageLink")));
        mapper.Apply(new(RecordedDomEventKind.Navigate, Url: "https://example.com/next", CausedByRecordedAction: true));

        var step = Assert.IsType<FindAndActAutomationStep>(Assert.Single(mapper.Build().Steps));
        Assert.IsType<ClickElementAction>(step.Action);
    }

    [Fact]
    public void RecordedEventMapper_ImagePick_EmitsFindElementsAndExtractSources()
    {
        var repeating = new RecordedEventMapper();
        repeating.Apply(new(RecordedDomEventKind.Pick, Pick: new RecordedPick(
            RecordedPickTool.Images,
            Repeating: true,
            ParentLocator: ElementLocator.Id("assetContainer"),
            DescendantTag: "img",
            TargetLocator: ElementLocator.CssSelector("img.asset-image"),
            VariableName: "images")));

        var repeatingPlan = repeating.Build();
        Assert.Equal(2, repeatingPlan.Steps.Count);
        var list = Assert.IsType<FindElementsChainAutomationStep>(repeatingPlan.Steps[0]);
        Assert.Equal("el1", list.RefName);
        Assert.Equal(ElementLocatorKind.Id, list.Chain.Segments[0].Kind);
        Assert.Equal("assetContainer", list.Chain.Segments[0].Value);
        Assert.Equal(ElementLocatorKind.TagName, list.Chain.Segments[1].Kind);
        Assert.Equal("img", list.Chain.Segments[1].Value);
        var sources = Assert.IsType<ExtractSourcesAutomationStep>(repeatingPlan.Steps[1]);
        Assert.Equal("el1", sources.ElementsListRefName);
        Assert.Equal("images", sources.VariableName);
        Assert.Equal(["src", "data-src", "data-lazy-src", "srcset"], sources.AttributeNames);

        var single = new RecordedEventMapper();
        single.Apply(new(RecordedDomEventKind.Pick, Pick: new RecordedPick(
            RecordedPickTool.Images,
            Repeating: false,
            ParentLocator: null,
            DescendantTag: "img",
            TargetLocator: ElementLocator.CssSelector("img.solo"),
            VariableName: "images")));

        var singlePlan = single.Build();
        var found = Assert.IsType<FindElementAutomationStep>(singlePlan.Steps[0]);
        Assert.Equal("img.solo", found.Locator.Value);
        var extract = Assert.IsType<ExtractElementDataAutomationStep>(singlePlan.Steps[1]);
        Assert.Equal(found.RefName, extract.ElementRefName);
        Assert.Equal("images", extract.VariableName);
        Assert.Equal(ElementDataExtractKind.Attribute, extract.Kind);
        Assert.Equal("src", extract.AttributeName);
        Assert.Equal(2, singlePlan.Steps.Count);
    }

    [Fact]
    public void RecordedEventMapper_GridLinkPick_EmitsFindElementsAndExtractHref()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Pick, Pick: new RecordedPick(
            RecordedPickTool.Links,
            Repeating: true,
            ParentLocator: ElementLocator.Id("grid"),
            DescendantTag: "a",
            TargetLocator: ElementLocator.CssSelector("a.row"),
            VariableName: "links")));

        var plan = mapper.Build();
        var list = Assert.IsType<FindElementsChainAutomationStep>(plan.Steps[0]);
        Assert.Equal("grid", list.Chain.Segments[0].Value);
        Assert.Equal(ElementLocatorKind.TagName, list.Chain.Segments[1].Kind);
        Assert.Equal("a", list.Chain.Segments[1].Value);
        var sources = Assert.IsType<ExtractSourcesAutomationStep>(plan.Steps[1]);
        Assert.Equal(list.RefName, sources.ElementsListRefName);
        Assert.Equal("links", sources.VariableName);
        Assert.Equal(["href"], sources.AttributeNames);
    }

    [Fact]
    public void RecordedEventMapper_EnterAfterTyping_EmitsInputThenSendKeys()
    {
        var mapper = new RecordedEventMapper();
        var locator = ElementLocator.CssSelector("input[name=\"q\"][placeholder=\"second\"]");
        mapper.Apply(new(RecordedDomEventKind.Click, "q", locator));
        mapper.Apply(new(RecordedDomEventKind.Input, "q", locator, Text: "z"));
        mapper.Apply(new(RecordedDomEventKind.Key, "q", locator, Text: "Enter"));

        var plan = mapper.Build();
        Assert.Equal(2, plan.Steps.Count);
        var input = Assert.IsType<InputTextElementAction>(Assert.IsType<FindAndActAutomationStep>(plan.Steps[0]).Action);
        Assert.Equal("z", input.Text);
        var keys = Assert.IsType<SendKeysElementAction>(Assert.IsType<FindAndActAutomationStep>(plan.Steps[1]).Action);
        Assert.Equal("Enter", keys.Keys);
        Assert.Equal(locator.Value, Assert.IsType<FindAndActAutomationStep>(plan.Steps[1]).Locator.Value);
    }

    [Fact]
    public void RecordedEventMapper_MoveAndRemove_ReordersCommittedSteps()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Click, "first", ElementLocator.Id("first")));
        mapper.Apply(new(RecordedDomEventKind.Click, "second", ElementLocator.Id("second")));
        mapper.Apply(new(RecordedDomEventKind.Blur, "second", ElementLocator.Id("second")));

        mapper.Move(1, 0);
        var moved = mapper.Preview();
        Assert.Equal("second", Assert.IsType<FindAndActAutomationStep>(moved.Steps[0]).Locator.Value);
        Assert.Equal("first", Assert.IsType<FindAndActAutomationStep>(moved.Steps[1]).Locator.Value);

        mapper.RemoveAt(0);
        var left = Assert.IsType<FindAndActAutomationStep>(Assert.Single(mapper.Preview().Steps));
        Assert.Equal("first", left.Locator.Value);
    }

    [Fact]
    public void RecordedEventMapper_ApplyJson_ReplacesSteps()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Click, "save", ElementLocator.Id("save")));
        var before = mapper.Build();
        Assert.IsType<FindAndActAutomationStep>(Assert.Single(before.Steps));

        var invalid = mapper.ApplyJson("{");
        Assert.False(invalid.Applied);
        Assert.False(string.IsNullOrWhiteSpace(invalid.Error));
        Assert.IsType<FindAndActAutomationStep>(Assert.Single(mapper.Build().Steps));

        const string json = """
                            {
                              "name": "edited",
                              "steps": [
                                { "type": "delay", "delayMilliseconds": 25 }
                              ]
                            }
                            """;
        var applied = mapper.ApplyJson(json);
        Assert.True(applied.Applied);
        Assert.Null(applied.Error);
        var delay = Assert.IsType<DelayAutomationStep>(Assert.Single(mapper.Build().Steps));
        Assert.Equal(25, delay.DelayMilliseconds);
    }

    [Fact]
    public void RecordedEventMapper_InsertStoreStringList_KeepsTheUrls()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Insert, Insert: new("storeStringList", "urls", Values: ["https://cdn.example/a.png", "https://cdn.example/b.png"])));

        var step = Assert.IsType<StoreStringListAutomationStep>(Assert.Single(mapper.Build().Steps));
        Assert.Equal("urls", step.VariableName);
        Assert.Equal(["https://cdn.example/a.png", "https://cdn.example/b.png"], step.Values);
    }

    [Fact]
    public void RecordedEventMapper_Reset_StartsAtTheUrlAndKeepsTheName()
    {
        var mapper = new RecordedEventMapper("named");
        mapper.Apply(new(RecordedDomEventKind.Click, "save", ElementLocator.Id("save")));
        mapper.Reset("https://example.com/next");

        var plan = mapper.Build();
        Assert.Equal("named", plan.Name);
        var step = Assert.IsType<NavigateAutomationStep>(Assert.Single(plan.Steps));
        Assert.Equal("https://example.com/next", step.Url);
    }

    [Fact]
    public void RecordedEventMapper_InsertNavigate_AddsANavigateStep()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Insert, Insert: new("navigate", Value: "https://example.com")));

        var step = Assert.IsType<NavigateAutomationStep>(Assert.Single(mapper.Build().Steps));
        Assert.Equal("https://example.com", step.Url);
    }

    [Fact]
    public void RecordedEventMapper_InsertReload_AddsAReloadStep()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Insert, Insert: new("reload")));

        Assert.IsType<ReloadAutomationStep>(Assert.Single(mapper.Build().Steps));
    }

    [Fact]
    public void RecordedEventMapper_InsertFindCss_AddsAFindElementStep()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Insert, Insert: new("findElement", Value: ".card", LocatorKind: ElementLocatorKind.CssSelector)));

        var step = Assert.IsType<FindElementAutomationStep>(Assert.Single(mapper.Build().Steps));
        Assert.Equal(ElementLocatorKind.CssSelector, step.Locator.Kind);
        Assert.Equal(".card", step.Locator.Value);
    }

    [Fact]
    public void RecordedEventMapper_InsertFindAndClick_AddsAFindAndActStep()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Insert, Insert: new(
            "findAndAct",
            Value: ".card",
            LocatorKind: ElementLocatorKind.CssSelector,
            Action: "click")));

        var step = Assert.IsType<FindAndActAutomationStep>(Assert.Single(mapper.Build().Steps));
        Assert.Equal(ElementLocatorKind.CssSelector, step.Locator.Kind);
        Assert.Equal(".card", step.Locator.Value);
        Assert.IsType<ClickElementAction>(step.Action);
    }

    [Fact]
    public void RecordedEventMapper_ResetAboutBlank_DropsSteps()
    {
        var mapper = new RecordedEventMapper();
        mapper.Apply(new(RecordedDomEventKind.Click, "save", ElementLocator.Id("save")));
        mapper.Reset("about:blank");
        Assert.Empty(mapper.Build().Steps);
    }
}
