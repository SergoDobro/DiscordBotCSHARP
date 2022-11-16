using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Audio.Streams;
using System.Diagnostics;
using System;
using System.IO;
using System.Net;
using System.Diagnostics.Metrics;
using System.Text;

internal class Program
{
    private DiscordSocketClient _client;
    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {

        _client = new DiscordSocketClient();

        _client.Log += Log;
        _client.MessageReceived += MessageHandler;
        _client.MessageUpdated += MessageUpdated;
        _client.VoiceServerUpdated += ConnectedToVoice;

        //  You can assign your bot token to a string, and pass that in to connect.
        //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
        var token = "NzU3MjQ4Mzc3MjA4NzAxMDA4.GmTDLK.BKqCCgpwWmvNcZs0yifw58YtkGaw4uPaPLyGRI";

        // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
        // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
        // var token = File.ReadAllText("token.txt");
        // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    Dictionary<ulong, AudioChat> songList = new Dictionary<ulong, AudioChat>();
    [Command(RunMode = RunMode.Async)]
    private async Task<Task> MessageHandler(SocketMessage msg)
    {
        if (msg.Author.IsBot)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        if (msg.Content.Contains("help") && msg.Content.Length<6)
        {
            msg.Channel.SendMessageAsync(text: "This is an experimental bot built because no reason and a challenge with c#. It does not use prefixes." +
                "\n/clear_queue to clear your queue" +
                "\n/clear_state if something is broken. " +
                "\n/add_next {link} to add song to queue." +
                "\n{link} to also add song to queue" +
                "It might be slow...");
        }
        
        Console.WriteLine(msg.ToString());


        var message = msg as SocketUserMessage;
        Console.WriteLine(message);
        if (msg.Author is SocketGuildUser)
        {
            if ((msg.Author as SocketGuildUser).VoiceChannel!=null)
            {
                try
                {
                    var voice = (msg.Author as SocketGuildUser).VoiceChannel;
                    if (msg.Content == "/clear_queue")
                    {
                        ulong chatid = msg.Channel.Id;
                        if (!songList.ContainsKey(chatid))
                            songList.Add(chatid, new AudioChat(chatid));
                        songList[chatid].ClearQueue();
                    }
                    else if (msg.Content == "/clear_state")
                    {
                        ulong chatid = msg.Channel.Id;
                        if (!songList.ContainsKey(chatid))
                            songList.Add(chatid, new AudioChat(chatid));
                        songList[chatid].ClearState();
                    }
                    else if (msg.Content == "/add_next ")
                    {
                        new Thread(async (v) =>
                        {
                            try
                            {
                                string song = msg.Content.Remove(0, "/add_next ".Length);
                                Uri uriResult;
                                bool result = Uri.TryCreate(song, UriKind.Absolute, out uriResult)
                                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                                if (result)
                                {
                                    ulong chatid = msg.Channel.Id;
                                    if (!songList.ContainsKey(chatid))
                                        songList.Add(chatid, new AudioChat(chatid));
                                    songList[chatid].AddNextSong(song);
                                    new Thread(() => { SendAsync(v as SocketVoiceChannel, songList[chatid]); }).Start();
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Troubles");
                                msg.Channel.SendMessageAsync(text: $"Troubles on the server(");
                            }
                        }).Start(voice);
                    }
                    else
                    {
                        new Thread(async (v) =>
                        {
                            try
                            {
                                Uri uriResult;
                                bool result = Uri.TryCreate(msg.Content, UriKind.Absolute, out uriResult)
                                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                                if (result)
                                {
                                    ulong chatid = msg.Channel.Id;
                                    if (!songList.ContainsKey(chatid))
                                        songList.Add(chatid, new AudioChat(chatid));
                                    songList[chatid].AddSong(msg.Content);
                                    new Thread(() => { SendAsync(v as SocketVoiceChannel, songList[chatid]); }).Start();
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Troubles");
                                msg.Channel.SendMessageAsync(text: $"Troubles on the server(");
                            }
                            ////WebClient Client = new WebClient();
                            ////string res = Client.DownloadString("https://www.youtube.com/oembed?format=json&url=https://www.youtube.com/watch?v="+msg.Content, "index.php");
                            ////if (true)
                            ////{

                            ////}

                        }).Start(voice);
                    }
                }
                catch
                {
                }
            }
        }
        return Task.CompletedTask;
    }
    private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        //git
        if (after.Author.IsBot)
        {
            Console.WriteLine(after.ToString());
            return;
        }
        // If the message was not in the cache, downloading it will result in getting a copy of `after`.
        var message = await before.GetOrDownloadAsync();
        Console.WriteLine($"{message.ToString()} -> {after.ToString()}");
        //after.Channel.SendMessageAsync(text: $"{message.Content} -> {after.Content}");
    }
    private async Task ConnectedToVoice(SocketVoiceServer voiceServer)
    {
    }

    //C:\\Users\\SergoDobro\\Desktop\\Für_Elise_Jam_The_Piano_Guys,_Людвиг_ван_Бетховен.mp3
    private string DownloadSong(string path)
    {
        string name = DateTime.Now.ToFileTime().ToString();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = System.Text.Encoding.Default;
        try
        {
            encoding = System.Text.Encoding.GetEncoding(1251);
            Console.WriteLine(1251);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            try
            {
                encoding = System.Text.Encoding.GetEncoding(866);
                Console.WriteLine(866);
            }
            catch (Exception ex1)
            {
                Console.WriteLine(ex1.Message);
                try
                {
                    encoding = System.Text.Encoding.GetEncoding(855);
                    Console.WriteLine(855);
                }
                catch (Exception ex2)
                {
                    Console.WriteLine(ex2.Message);
                }
            }
        }
        ////foreach (var encc in Encoding.GetEncodings())
        ////{
        ////    var procc = new Process
        ////    {
        ////        StartInfo = new ProcessStartInfo
        ////        {
        ////            FileName = "yt-dlp.exe",
        ////            Arguments = $"-e {path}",
        ////            UseShellExecute = false,
        ////            RedirectStandardOutput = true,
        ////            RedirectStandardError = false,
        ////            StandardOutputEncoding = Encoding.GetEncoding(encc.CodePage)
        ////}
        ////    };
        ////    Console.WriteLine(encc.CodePage);
        ////    procc.Start();
        ////    procc.WaitForExit(); 
        ////    name = procc.StandardOutput.ReadToEnd();
        ////    name = name.Remove(name.Length - 1);
        ////    name = name.Replace(" ", "_");
        ////    name = name.Replace("'", "-");
        ////    name = name.Replace("\"", "-");
        ////    name = name.Replace("|", "#");
        ////    Console.WriteLine(name);
        ////}
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp.exe",
                Arguments = $"-e {path}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                StandardOutputEncoding = encoding
            }
        };
        proc.Start();
        proc.WaitForExit();
        name = proc.StandardOutput.ReadToEnd();
        name = name.Remove(name.Length - 1);
        name = name.Replace(" ", "_");
        name = name.Replace("'", "-");
        name = name.Replace("\"", "-");
        name = name.Replace("|", "#");
        Console.WriteLine(name);

        Console.WriteLine(path + "downloading starts");
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "yt-dlp.exe",
                    Arguments = $"-i -c -x --audio-format mp3 --audio-quality 0 --output {name}.mp3 {path}",
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,

                }
                ).WaitForExit();
        }
        catch
        {
        }
        Console.WriteLine(path + "downloading ended");
        if (!File.Exists($"{name}.mp3"))
        {
            try
            {
                var prc = Process.Start(new ProcessStartInfo
                {
                    FileName = "yt-dlp.exe",
                    Arguments = $"-i -c -x --audio-format mp3 --audio-quality 0 --output {name}.mp3 {path}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,

                });
                proc.WaitForExit();
            }
            catch
            {
            }
        }
        return name;
    }
    private Process CreateStream(string name)
    {

        var info2 = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{name}.mp3\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        Thread.Sleep(50);
        Thread.Sleep(50);
        return Process.Start(info2);
    }
    private async void SendAsync(SocketVoiceChannel vo, AudioChat audioChat)
    {
        //using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
        //{
        //    Console.WriteLine(discord.CanWrite);
        //    if (discord.CanWrite)
        //    {
        //        audioChat.Status = AudioChatStatus.None;
        //    }
        //}

        if (audioChat.Status == AudioChatStatus.Playing)
            return;
        if (vo is null)
            return;
        audioChat.Status = AudioChatStatus.Playing;
        var client = await (vo.ConnectAsync());


        using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
        {

            while (audioChat.SongList.Count > 0)
            {
                try
                {
                    string path = audioChat.PullOutSong();
                    Console.WriteLine("a");
                    // Create FFmpeg using the previous example
                    string name = DownloadSong(path);
                    using (var ffmpeg = CreateStream(name))
                    {

                        Thread.Sleep(50);
                        using (var output = ffmpeg.StandardOutput.BaseStream)

                            try { await output.CopyToAsync(discord); }
                            finally { await discord.FlushAsync(); }
                    }
                    Console.WriteLine("b");
                }
                catch
                {
                    audioChat.Status = AudioChatStatus.None;
                    if (audioChat.SongList.Count > 0)
                    {

                    }
                }
                finally
                {
                    Console.WriteLine("c");
                }
            }
        }
        audioChat.Status = AudioChatStatus.None;
    }

}
public enum AudioChatStatus { Playing, None }
class AudioChat
{
    public AudioChat(ulong id)
    {
        Id = Id;
    }
    public ulong Id { get; set; }
    public List<string> SongList { get; set; } = new List<string>();
    public AudioChatStatus Status { get; set; } = AudioChatStatus.None;
    public void AddSong(string song)
    {
        SongList.Add(song);
    }
    public void AddNextSong(string song)
    {
        SongList.Insert(0, song);
    }
    public void ClearQueue()
    {
        SongList.Clear();
        Status = AudioChatStatus.None;
    }
    public void ClearState()
    {
        Status = AudioChatStatus.None;
    }
    public string PullOutSong()
    {
        if (SongList.Count == 0)
            return "none";
        string song = SongList.FirstOrDefault();
        SongList.RemoveAt(0);
        return song;
    }
}

