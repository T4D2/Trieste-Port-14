using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Traitor.Components;
using Content.Shared.Paper;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Traitor.Systems;

public sealed class TraitorCodePaperSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRuleSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TraitorCodePaperComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    ///     This method sets everything up when the paper is spawned.
    /// </summary>
    /// <param name="codePaperUid">TraitorCodePaper Uid</param>
    /// <param name="codePaperComp">TraitorCodePaper Comp</param>
    /// <param name="args">MapInitEvent Arguments</param>
    private void OnMapInit(EntityUid codePaperUid, TraitorCodePaperComponent codePaperComp, MapInitEvent args)
    {
        SetupPaper(codePaperUid, codePaperComp);
    }

    /// <summary>
    ///     This method sets up the traitor paper contents.
    /// </summary>
    /// <param name="codePaperUid"></param>
    /// <param name="codePaperComp"></param>
    private void SetupPaper(EntityUid codePaperUid, TraitorCodePaperComponent? codePaperComp = null)
    {
        if (!Resolve(codePaperUid, ref codePaperComp))
            return;

        if (TryComp(codePaperUid, out PaperComponent? paperComp))
        {
            if (TryGetTraitorCode(out var paperContent, codePaperComp))
            {
                _paper.SetContent((codePaperUid, paperComp), paperContent);
            }
        }
    }

    /// <summary>
    ///     Try to get the traitor codewords from the game rules.
    /// </summary>
    /// <param name="traitorCode">Traitor Code (out)</param>
    /// <param name="codePaperComp">TraitorCodePaper Component</param>
    /// <returns></returns>
    private bool TryGetTraitorCode([NotNullWhen(true)] out string? traitorCode, TraitorCodePaperComponent codePaperComp)
    {
        traitorCode = null;
        var codesMessage = new FormattedMessage();
        List<string> codeList = new();

        // First, we check if the Traitor gamerule has been added.
        // If it has, get the traitor component and then check if it's NT or Syndicate.
        // Then we add them to the codelist range.
        if (_gameTicker.IsGameRuleAdded<TraitorRuleComponent>())
        {
            var rules = _gameTicker.GetAddedGameRules();
            foreach (var ruleEnt in rules)
            {
                if (TryComp(ruleEnt, out TraitorRuleComponent? traitorComp))
                {
                    var codewords = codePaperComp.CodewordFaction == "NanoTrasen"
                        ? traitorComp.NanoTrasenCodewords
                        : traitorComp.SyndicateCodewords;

                    codeList.AddRange(codewords.ToList());
                }
            }
        }

        // Now we check if the codelist is empty.
        // If it is, we check if we can add fake codewords for the faction and do so.
        // Otherwise, we set it to none.
        if (codeList.Count == 0)
        {
            if (codePaperComp.FakeCodewords)
            {
                codeList = _traitorRuleSystem.GenerateTraitorCodewords(
                        new TraitorRuleComponent(),
                        codePaperComp.CodewordFaction)
                    .ToList();
            }
            else
            {
                codeList = [Loc.GetString("traitor-codes-none")];
            }
        }

        // Now we shuffle the codelist to be in a random order.
        // Then we count the codewords. Every code is a new line on the paper.
        _random.Shuffle(codeList);

        var i = 0;
        foreach (var code in codeList)
        {
            i++;
            if (i > codePaperComp.CodewordAmount && !codePaperComp.CodewordShowAll)
                break;

            codesMessage.PushNewline();
            codesMessage.AddMarkupOrThrow(code);
        }

        // Finally, we check if the codeword count is one or more,
        // and based on that we set the paper to be plural or singular.
        if (!codesMessage.IsEmpty)
        {
            if (i == 1)
                traitorCode = Loc.GetString("traitor-codes-message-singular") + codesMessage;
            else
                traitorCode = Loc.GetString("traitor-codes-message-plural") + codesMessage;
        }

        return !codesMessage.IsEmpty;
    }
}
