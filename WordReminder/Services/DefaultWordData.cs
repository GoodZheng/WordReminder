using WordReminder.Models;

namespace WordReminder.Services;

public static class DefaultWordData
{
    private static readonly Dictionary<string, (string Phonetic, string Pos, string Definition, string Example)> Words = new()
    {
        ["abandon"] = ("əˈbændən", "v.", "放弃，遗弃，抛弃", "He abandoned his car and ran for help."),
        ["ability"] = ("əˈbɪləti", "n.", "能力，才能，本领", "She has the ability to solve complex problems."),
        ["absence"] = ("ˈæbsəns", "n.", "缺席，不在，缺乏", "His absence from the meeting was noticed."),
        ["absolute"] = ("ˈæbsəluːt", "adj.", "绝对的，完全的，十足的", "I have absolute confidence in your ability."),
        ["abstract"] = ("ˈæbstrækt", "adj.", "抽象的，理论上的", "Truth and beauty are abstract concepts."),
        ["academic"] = ("ˌækəˈdemɪk", "adj.", "学术的，学院的，理论的", "She excelled in her academic studies."),
        ["accept"] = ("əkˈsept", "v.", "接受，认可，同意", "Please accept my sincere apology."),
        ["access"] = ("ˈækses", "n.", "进入，通道，使用权限", "Students have access to the library."),
        ["accident"] = ("ˈæksɪdənt", "n.", "事故，意外，偶然", "The accident happened yesterday morning."),
        ["accompany"] = ("əˈkʌmpəni", "v.", "陪伴，陪同，伴随", "She accompanied me to the hospital."),
        ["accomplish"] = ("əˈkʌmplɪʃ", "v.", "完成，实现，达到", "We accomplished our mission on time."),
        ["according"] = ("əˈkɔːrdɪŋ", "adv.", "根据，按照，取决于", "According to the weather forecast, it will rain."),
        ["account"] = ("əˈkaʊnt", "n.", "账户，账目，解释", "I need to open a bank account."),
        ["accurate"] = ("ˈækjərət", "adj.", "精确的，准确的，正确无误的", "The report was accurate and well-researched."),
        ["accuse"] = ("əˈkjuːz", "v.", "指责，指控，控告", "They accused him of stealing the money."),
        ["achieve"] = ("əˈtʃiːv", "v.", "达到，实现，完成", "You can achieve anything if you work hard."),
        ["achievement"] = ("əˈtʃiːvmənt", "n.", "成就，成绩，完成", "Winning the championship was a great achievement."),
        ["acid"] = ("ˈæsɪd", "n.", "酸，酸性物质", "Vinegar contains acetic acid."),
        ["acknowledge"] = ("əkˈnɑːlɪdʒ", "v.", "承认，致谢，确认收到", "He refused to acknowledge his mistake."),
        ["acquire"] = ("əˈkwaɪər", "v.", "获得，取得，学到", "She acquired new skills during the training.")
    };

    public static Word GetWord(string text)
    {
        if (Words.TryGetValue(text.ToLower(), out var data))
        {
            return new Word
            {
                Text = text,
                Phonetic = $"[{data.Phonetic}]",
                PartOfSpeech = data.Pos,
                Definition = data.Definition,
                Example = data.Example
            };
        }

        return new Word { Text = text };
    }

    public static bool HasWord(string text) => Words.ContainsKey(text.ToLower());

    public static IEnumerable<string> GetAllWords() => Words.Keys;
}
