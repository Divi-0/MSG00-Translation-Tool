using MSG00.Translation.Application.Features.Font;
using System.Text;
using System.Text.RegularExpressions;

namespace MSG00.Translation.Application.Features.Arm9;

/// <summary>A text inside the ARM9 program, referenced by exactly one pointer (see docs/arm9/README.md).</summary>
/// <param name="Offset">offset in the ARM9 binary (RAM address - 0x02000000)</param>
public sealed record Arm9TextEntry(int Offset, string Section, string Label, string Japanese);

/// <summary>
/// Texts in the ARM9 can only be replaced in place: the new text must fit into the bytes up to the next string
/// (the original text, its NUL and the zero padding). The rest of the room is filled with zeros.
/// </summary>
public static partial class Arm9Texts
{
    public const string MainMenu = "Main menu";

    public const string InGameMenu = "In-game menu";

    public const string FieldSelect = "Field select";

    public const string SaveLoad = "Save / Load";

    public const string DialogButtons = "Dialog buttons";

    public const string PauseMenu = "Pause menu";

    public const string MissionResults = "Mission results";

    public const string PilotSelect = "Pilot select";

    public const string ButtonConfig = "Button config";

    public const string TacticalSupport = "Tactical support";

    private static readonly Encoding ShiftJis = GameEncoding.Instance;

    /// <summary>
    /// Known texts; a fixed list so resource names in the same area (e.g. "SELECT_MOBJ00") are never edited.
    /// </summary>
    public static IReadOnlyList<Arm9TextEntry> Entries { get; } =
    [
        // MODE SELECT after the title; the buttons (NEW GAME, LOAD GAME, GUNPLA Navi) are graphics and already English.
        // Each help text exists in several tables, all are used.
        new(0xB3804, MainMenu, "Help: new game", "新しくゲームを始めます。"),
        new(0xB3880, MainMenu, "Help: new game (2nd table)", "新しくゲームを始めます。"),
        new(0xB3910, MainMenu, "Help: new game (3rd table)", "新しくゲームを始めます。"),
        new(0xB3B9C, MainMenu, "Help: new game (4th table)", "新しくゲームを始めます。"),
        new(0xB38AC, MainMenu, "Help: load game", "つづきからゲームを始めます。"),
        new(0xB393C, MainMenu, "Help: load game (2nd table)", "つづきからゲームを始めます。"),
        new(0xB38DC, MainMenu, "Help: Gunpla Navi", "ガンプラの作り方を教えてくれます。"),
        new(0xB396C, MainMenu, "Help: Gunpla Navi (2nd table)", "ガンプラの作り方を教えてくれます。"),
        new(0xB3BC4, MainMenu, "Error: save data could not be read (3 lines, end with a line break)", "データを読めませんでした。\n電源を切って、カードを\n差し込み直して下さい。\n"),

        new(0xB9CFC, InGameMenu, "Menu: mission select", "ミッション選択"),
        new(0xB9D30, InGameMenu, "Menu: save", "ＳＡＶＥ"),
        new(0xB9D3C, InGameMenu, "Menu: load", "ＬＯＡＤ"),
        new(0xB9D0C, InGameMenu, "Menu: button config", "ボタンコンフィグ"),
        new(0xB9D20, InGameMenu, "Menu: back to title", "タイトルに戻る"),
        new(0xB9C98, InGameMenu, "Help: mission select", "ミッションを選択します"),
        new(0xB9CDC, InGameMenu, "Help: save", "セーブします"),
        new(0xB9CEC, InGameMenu, "Help: load", "ロードします"),
        new(0xB9CB0, InGameMenu, "Help: button config", "操作方法を設定します"),
        new(0xB9CC8, InGameMenu, "Help: back to title", "タイトルに戻ります"),
        new(0xB9D68, InGameMenu, "Help (2nd table): mission select", "ミッションを選択します"),
        new(0xB9DAC, InGameMenu, "Help (2nd table): save", "セーブします"),
        new(0xB9DBC, InGameMenu, "Help (2nd table): load", "ロードします"),
        new(0xB9D80, InGameMenu, "Help (2nd table): button config", "操作方法を設定します"),
        new(0xB9D98, InGameMenu, "Help (2nd table): back to title", "タイトルに戻ります"),
        new(0xB9DD8, FieldSelect, "Help: select the map", "出撃先のマップを選択してください"),
        new(0xB9EA4, FieldSelect, "Button help (screen not confirmed)", "Ａボタン：決定　Ｂボタン：戻る"),

        // LOAD / SAVE screen (from the main menu and the in-game menu)
        new(0xBACC8, SaveLoad, "Help: select the data to load", "ロードする箇所を選択して下さい。"),
        new(0xBACA4, SaveLoad, "Help: select the data to save", "セーブする箇所を選択して下さい。"),
        new(0xBAC4C, SaveLoad, "Slot info: days / tactical support (keep %02d and %03d)", "%02ddays       取得した戦術支援 %03d%"),
        new(0xBAD4C, SaveLoad, "Slot info (2nd table, keep %02d and %03d)", "%02ddays       取得した戦術支援 %03d%"),
        new(0xBAD00, SaveLoad, "Message: loaded", "ロードしました。"),
        new(0xBACEC, SaveLoad, "Message: saved", "セーブしました。"),
        // question table: 128 bytes per entry from 0xBA7BC, pointer at 0x9D35C
        new(0xBA7BC, SaveLoad, "Question: save?", "セーブしますか？"),
        new(0xBA83C, SaveLoad, "Question: overwrite the save file?", "セーブファイルを上書きしますか？"),
        new(0xBA8BC, SaveLoad, "Question: quit saving?", "セーブを終了しますか？"),
        new(0xBA93C, SaveLoad, "Question: load DATA-01?", "ＤＡＴＡ−０１をロードしますか？"),
        new(0xBA9BC, SaveLoad, "Question: load DATA-02?", "ＤＡＴＡ−０２をロードしますか？"),
        new(0xBAA3C, SaveLoad, "Question: load DATA-03?", "ＤＡＴＡ−０３をロードしますか？"),
        new(0xBAABC, SaveLoad, "Question: quit loading?", "ロードを終了しますか？"),
        new(0xBADAC, SaveLoad, "Error: data could not be written (3 lines, end with a line break)", "データを書けませんでした。\n電源を切って、カードを\n差し込み直して下さい。\n"),
        new(0xBADF8, SaveLoad, "Error: data could not be read (3 lines, end with a line break)", "データを読めませんでした。\n電源を切って、カードを\n差し込み直して下さい。\n"),

        // buttons of the yes/no dialogs, three sets (which dialog uses which set is not known)
        new(0xB33F0, DialogButtons, "YES (set 1)", "ＹＥＳ"),
        new(0xB33F8, DialogButtons, "NO (set 1)", "ＮＯ"),
        new(0xB3400, DialogButtons, "OK (set 1)", "ＯＫ"),
        new(0xB340C, DialogButtons, "YES (set 2)", "ＹＥＳ"),
        new(0xB3414, DialogButtons, "NO (set 2)", "ＮＯ"),
        new(0xB341C, DialogButtons, "YES (set 3)", "ＹＥＳ"),
        new(0xB3424, DialogButtons, "NO (set 3)", "ＮＯ"),

        // pause menu during a battle (START)
        new(0xB4A9C, PauseMenu, "Menu: victory conditions", "勝利条件確認"),
        new(0xB4AAC, PauseMenu, "Menu: retreat", "撤退する"),
        new(0xB4AB8, PauseMenu, "Menu: back to title", "タイトル画面に戻る"),
        new(0xB4A14, PauseMenu, "Help: victory conditions", "勝利条件を確認します。"),
        new(0xB4A2C, PauseMenu, "Help: retreat", "戦闘を切り上げ、撤退します。"),
        new(0xB4A4C, PauseMenu, "Help: back to title (3 lines)", "ゲームを終了し、タイトル画面に\n戻ります。現在行っている戦闘は\n記録されません。"),
        new(0xB4B20, PauseMenu, "Help (2nd table): victory conditions", "勝利条件を確認します。"),
        new(0xB4B38, PauseMenu, "Help (2nd table): retreat", "戦闘を切り上げ、撤退します。"),
        new(0xB4B58, PauseMenu, "Help (2nd table): back to title (3 lines)", "ゲームを終了し、タイトル画面に\n戻ります。現在行っている戦闘は\n記録されません。"),
        new(0xB48AC, PauseMenu, "Question: retreat? (lines start with a line break / space)", "\n 日数が経過し、敵勢力も若干上昇して\n しまいますが、撤退しますか？"),
        new(0xB48F0, PauseMenu, "Question: back to title? (lines start with a space)", " タイトル画面に戻ります。\n セーブした所から再開する事に\n なりますがよろしいですか？"),
        new(0xB49F4, PauseMenu, "Buttons: A confirm, B/START back", "Ａ：決定　Ｂ／ＳＴＡＲＴ:戻る"),
        new(0xB4AD8, PauseMenu, "Buttons: B back", "Ｂ:戻る"),
        new(0xB4AEC, PauseMenu, "Buttons: A confirm, B back", "Ａ：決定　Ｂ:戻る"),
        new(0xB4B0C, PauseMenu, "Buttons: A confirm, B back (2nd table)", "Ａ：決定　Ｂ:戻る"),

        // mission results: message table with 128 bytes per entry from 0xB5D20 (pointer at 0x88354), save question
        new(0xB5D20, MissionResults, "Message: Tactical support result: Exia", "【戦術支援・結果】\nエクシアの活躍によって、\nＡＥＵの勢力が減少しました。"),
        new(0xB5DA0, MissionResults, "Message: Tactical support result: Dynames", "【戦術支援・結果】\nデュナメスの活躍によって、\nユニオンの勢力が減少しました。"),
        new(0xB5E20, MissionResults, "Message: Tactical support result: Kyrios", "【戦術支援・結果】\nキュリオスの活躍によって、\n人革連の勢力が減少しました。"),
        new(0xB5EA0, MissionResults, "Message: Tactical support result: Virtue", "【戦術支援・結果】\nヴァーチェの活躍によって、\n全勢力が少しずつ減少しました。"),
        new(0xB5F20, MissionResults, "Message: Tactical support result: agent", "【戦術支援・結果】\nエージェントの工作が成功しました。\nこれにより、１番高い勢力が２番目の\n勢力と同じ状態まで減少しました。"),
        new(0xB5FA0, MissionResults, "Message: Tactical support result: Marina", "【戦術支援・結果】\nマリナ王女の政治交渉によって、\n本作戦における勢力の減少率が\n２０％上昇しました。"),
        new(0xB6020, MissionResults, "Message: Tactical support failed: agent", "【戦術支援・失敗】\n本作戦の失敗とともに、エージェントの\n工作活動も失敗に終わりました。"),
        new(0xB60A0, MissionResults, "Message: Tactical support failed: Marina", "【戦術支援・失敗】\n本作戦の失敗とともに、マリナ王女の\n政治交渉も失敗に終わりました。"),
        new(0xB6120, MissionResults, "Message: Unlocked: Setsuna & Exia", "戦術支援「刹那＆エクシア」\nが使用可能となりました。"),
        new(0xB61A0, MissionResults, "Message: Unlocked: Lockon & Dynames", "戦術支援「ロックオン＆デュナメス」\nが使用可能となりました。"),
        new(0xB6220, MissionResults, "Message: Unlocked: Allelujah & Kyrios", "戦術支援「アレルヤ＆キュリオス」\nが使用可能となりました。"),
        new(0xB62A0, MissionResults, "Message: Unlocked: Tieria & Virtue", "戦術支援「ティエリア＆ヴァーチェ」\nが使用可能となりました。"),
        new(0xB6320, MissionResults, "Message: Unlocked: Sumeragi", "戦術支援「スメラギ」\nが使用可能となりました。"),
        new(0xB63A0, MissionResults, "Message: Unlocked: Karel", "戦術支援「カレル」\nが使用可能となりました。"),
        new(0xB6420, MissionResults, "Message: Unlocked: Christina & Feldt", "戦術支援「クリスティナ＆フェルト」\nが使用可能となりました。"),
        new(0xB64A0, MissionResults, "Message: Unlocked: Haro", "戦術支援「ハロ」\nが使用可能となりました。"),
        new(0xB6520, MissionResults, "Message: Unlocked: Wang Liu Mei", "戦術支援「王留美」\nが使用可能となりました。"),
        new(0xB65A0, MissionResults, "Message: Unlocked: Marina", "戦術支援「マリナ」\nが使用可能となりました。"),
        new(0xB6620, MissionResults, "Message: Unlocked: Aeolia", "戦術支援「イオリア」\nが使用可能となりました。"),
        new(0xB66A0, MissionResults, "Message: Unlocked: Saji & Louise", "戦術支援「沙慈＆ルイス」\nが使用可能となりました"),
        new(0xB6720, MissionResults, "Message: Tactical supports: 2", "戦術支援の実行回数が２になりました。\n出撃前に２つ戦術支援をセットできます。"),
        new(0xB67A0, MissionResults, "Message: Tactical supports: 3", "戦術支援の実行回数が３になりました。\n出撃前に３つ戦術支援をセットできます。"),
        new(0xB6820, MissionResults, "Message: Tactical supports: 4", "戦術支援の実行回数が４になりました。\n出撃前に４つ戦術支援をセットできます。"),
        new(0xB68A0, MissionResults, "Message: New mission offered", "新たなミッションが提示されました。"),
        new(0xB6964, MissionResults, "Question: save?", "セーブしますか？"),

        // BUTTON CONFIG (in-game menu): description of the selected control type, 192-byte entries
        new(0xAF77A, ButtonConfig, "Help: type A", "基本となる操作形態です。\nＹボタン、Ｂボタンで上下移動を行います。"),
        new(0xAF83A, ButtonConfig, "Help: type B", "タイプＡを基本とし、Ｙボタン、Ｂボタンが\n攻撃系、Ｘボタン、Ａボタンが上下移動と\nなります。"),
        new(0xAF8FA, ButtonConfig, "Help: type C", "Ｌボタン、Ｒボタンが上下移動となり、\nＹボタン、Ｂボタンがロックオン関連と\nなります。キュリオスの変形は\nＬボタンとＲボタンの同時押しになります。"),
        new(0xAF9BA, ButtonConfig, "Help: type D", "タイプＣを基本とし、Ｙボタン、Ｂボタンが\n攻撃系、Ｘボタン、Ａボタンがロックオン\n関連となります。"),
        new(0xB8E88, ButtonConfig, "Note: save the setting", "変更した内容を次回プレイ時にも\n反映させる場合は、\nブリーフィングメニューの\n「SAVE」で記録します。"),

        // questions of the BRIEFING menu
        new(0xB8DF8, InGameMenu, "Question: back to title?", "セーブしたところから再開することに\nなりますがタイトルに戻りますか？"),
        new(0xB8E3C, InGameMenu, "Question: choose MS / pilot again?", "セットした「戦術支援」が解除されます。\nＭＳ／パイロットを選びなおしますか？"),

        // TACTICAL SUPPORT (pilot select → Support): names in the list (32-byte entries), description of the selected
        // support and the message when it is used (256-byte entries, same order as the names)
        .. TacticalSupportEntries(),

        // PILOT SELECT stats: TYPE (pilot type) and STRONG VS. (faction); 16-byte table entries
        new(0xB7120, PilotSelect, "Type: melee", "格闘"),
        new(0xB7130, PilotSelect, "Type: shooting", "射撃"),
        new(0xB7140, PilotSelect, "Type: all-round", "万能"),
        new(0xB7150, PilotSelect, "Type: power", "パワー"),
        new(0xB7160, PilotSelect, "Strong vs.: AEU", "ＡＥＵ"),
        new(0xB7170, PilotSelect, "Strong vs.: Union", "ユニオン"),
        new(0xB7180, PilotSelect, "Strong vs.: HRL", "人革連"),
        new(0xB7190, PilotSelect, "Strong vs.: all", "全て"),
    ];

    private static IEnumerable<Arm9TextEntry> TacticalSupportEntries()
    {
        (string Name, string Japanese, string Description, string Used)[] supports =
        [
            ("Setsuna & Exia", "刹那＆エクシア", "エクシアが同時に別ポイントでの\nＡＥＵミッションに出撃します。", "【戦術支援・発動】\nエクシアが別ポイントでの\nＡＥＵミッションに同時出撃します。"),
            ("Lockon & Dynames", "ロックオン＆デュナメス", "デュナメスが同時に別ポイントでの\nユニオンミッションに出撃します。", "【戦術支援・発動】\nデュナメスが別ポイントでの\nユニオンミッションに同時出撃します。"),
            ("Allelujah & Kyrios", "アレルヤ＆キュリオス", "キュリオスが同時に別ポイントでの\n人革連ミッションに出撃します。", "【戦術支援・発動】\nキュリオスが別ポイントでの\n人革連ミッションに同時出撃します。"),
            ("Tieria & Virtue", "ティエリア＆ヴァーチェ", "ヴァーチェが同時に別ポイントでの\n対テロミッションに出撃します。", "【戦術支援・発動】\nヴァーチェが別ポイントでの\n対テロミッションに同時出撃します。"),
            ("Sumeragi", "スメラギ", "整備を最優先とし、出撃までに\n全ガンダムのＨＰを全回復します。", "【戦術支援・発動】\nスメラギ・李・ノリエガの指示により、\n全ガンダムの整備が完了。ＨＰ全回復。"),
            ("Karel", "カレル", "現在ＨＰの一番低いガンダムを\n出撃までに全回復させます。", "【戦術支援・発動】\n整備ロボット「カレル」の整備作業で、\nＨＰの一番低かったガンダムが全回復。"),
            ("Christina & Feldt", "クリスティナ＆フェルト", "出撃するガンダムのダッシュ中に消費する\nスラスター量を２０％減少させます。", "【戦術支援・発動】\n出撃するガンダムのダッシュ中に\n消費するスラスター量を２０％\n減少させます。"),
            ("Haro", "ハロ", "出撃するガンダムの戦闘中に受ける\nダメージ量を２０％減少させます。", "【戦術支援・発動】\n出撃するガンダムの戦闘中に受ける\nダメージ量を２０％減少させます。"),
            ("Wang Liu Mei", "王留美", "本作戦の成功時に、エージェントの工作\nにより、現在１番高い勢力を２番目の勢力\nと同じ状態に減少させます。", "【戦術支援・発動】\n本作戦の成功時に、エージェントの工作\nにより、現在１番高い勢力を２番目の\n勢力と同じ状態に減少させます。"),
            ("Marina", "マリナ", "本作戦が成功した場合に限って、\n作戦成功時に減少する勢力影響値を\n２０％上昇させます。", "【戦術支援・発動】\n本作戦が成功した場合に限って、\n作戦成功時に減少する勢力影響値を\n２０％上昇させます。"),
            ("Aeolia", "イオリア", "出撃するガンダムの攻撃力を一律で\n２０％上昇させます。", "【戦術支援・発動】\n出撃するガンダムの攻撃力を一律で\n２０％上昇させます。"),
            ("Saji & Louise", "沙慈＆ルイス", "出撃するガンダムのテンションゲージの\n上昇率を２０％増加させます。", "【戦術支援・発動】\n出撃するガンダムのテンションゲージの\n上昇率を２０％増加させます。"),
        ];

        for (var i = 0; i < supports.Length; i++)
        {
            yield return new(0xB7218 + i * 0x20, TacticalSupport, $"Name: {supports[i].Name}", supports[i].Japanese);
        }

        // messages when a support can not be set
        yield return new(0xB973C, TacticalSupport, "Message: not in list view", "リスト表示では設定できません");
        yield return new(0xB975C, TacticalSupport, "Message: same pilot as sortie", "出撃パイロットと同一の\n戦術支援は選べません\n");
        yield return new(0xB978C, TacticalSupport, "Message: not available", "使用可能な状態ではありません\n");
        yield return new(0xB97AC, TacticalSupport, "Message: no uses left", "これ以上使用できません");

        yield return new(0xB7398, TacticalSupport, "Hidden units: unlocked", "【隠し機体が選択可能です】\n「エクシア・ロールアウトカラー」と\n「ナドレ」の２機が新たに使用可能。");
        yield return new(0xB7458, TacticalSupport, "Hidden units: how to select", "刹那とティエリアの選択画面にて、十字\nボタン下を押しながらＡボタンで決定。\nこれによりミッション内で操作する機体\nが隠し機体に変更されます。");

        for (var i = 0; i < supports.Length; i++)
        {
            yield return new(0xB7518 + i * 0x100, TacticalSupport, $"Description: {supports[i].Name}", supports[i].Description);
        }

        for (var i = 0; i < supports.Length; i++)
        {
            yield return new(0xB8118 + i * 0x100, TacticalSupport, $"Used: {supports[i].Name}", supports[i].Used);
        }
    }

    public static IReadOnlyList<string> Sections { get; } = Entries.Select(x => x.Section).Distinct().ToList();

    public static string Read(byte[] arm9, Arm9TextEntry entry)
    {
        var end = Array.IndexOf(arm9, (byte)0, entry.Offset);
        return ShiftJis.GetString(arm9, entry.Offset, end - entry.Offset);
    }

    /// <summary>bytes available for the text, without the terminating NUL</summary>
    public static int MaxBytes(byte[] arm9, Arm9TextEntry entry)
    {
        var next = Array.IndexOf(arm9, (byte)0, entry.Offset);
        while (next < arm9.Length && arm9[next] == 0)
        {
            next++;
        }

        return next - entry.Offset - 1;
    }

    public static int ByteCount(string text) => ShiftJis.GetByteCount(text);

    /// <summary>printf placeholders like %02d; the game fills them in, so a translation must keep them in the same order</summary>
    public static IReadOnlyList<string> Placeholders(string text) => PlaceholderPattern().Matches(text).Select(x => x.Value).ToList();

    public static bool HasSamePlaceholders(Arm9TextEntry entry, string text) => Placeholders(entry.Japanese).SequenceEqual(Placeholders(text));

    /// <summary>writes <paramref name="text"/> in place and fills the rest of the room with zeros</summary>
    public static void Write(byte[] arm9, Arm9TextEntry entry, string text)
    {
        var maxBytes = MaxBytes(arm9, entry);
        var bytes = ShiftJis.GetBytes(text);
        if (bytes.Length > maxBytes)
        {
            throw new ArgumentException($"\"{text}\" is {bytes.Length} bytes, only {maxBytes} fit at 0x{entry.Offset:X}", nameof(text));
        }

        if (!HasSamePlaceholders(entry, text))
        {
            throw new ArgumentException($"\"{text}\" must contain the placeholders {string.Join(" ", Placeholders(entry.Japanese))} in this order", nameof(text));
        }

        Array.Clear(arm9, entry.Offset, maxBytes + 1);
        bytes.CopyTo(arm9, entry.Offset);
    }

    [GeneratedRegex(@"%[-+ #0]*\d*(?:\.\d+)?[diouxXcs]")]
    private static partial Regex PlaceholderPattern();
}