using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.GameData.Getters.Actions;

/// <summary>
/// Class for getting action IDs from the Excel sheet.
/// </summary>
internal class ActionIdGetter : ActionGetterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActionIdGetter"/> class.
    /// </summary>
    /// <param name="gameData">The game data.</param>
    public ActionIdGetter(Lumina.GameData gameData)
        : base(gameData)
    {
    }

    /// <summary>
    /// Converts the specified action to its code representation.
    /// </summary>
    /// <param name="item">The action to convert.</param>
    /// <returns>The code representation of the action.</returns>
    protected override string ToCode(Action item)
    {
        var name = GetName(item);

        return $"""
        /// <summary>
        /// {item.GetDescName()}
        /// {GetDesc(item)}
        /// </summary>
        {name} = {item.RowId},
        """;
    }
}
