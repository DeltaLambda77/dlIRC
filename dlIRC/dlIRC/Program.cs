using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace dlIRC
{
    class Program
    {
        public class ConnectionParameters
        {
            public string serverIP;
            public int serverPort;
            public string username;
            public string password;
            public string serverChannel;
        }

        static class ErrorVariables
        {
            public static int responseNumber;
        }

        enum ErrorCodes
        {
            ErrNoSuchNick = 401,                    // <nickname> :No such nick
            ErrNoSuchServer = 402,                  // <server> :No such server
            ErrNoSuchChannel = 403,                 // <channel> :No such channel
            ErrCannotSendToChan = 404,              // <channel> :Cannot send to channel
            ErrTooManyChannels = 405,               // <channel> :You have joined too many channels
            ErrWasNoSuchNick = 406,                 // <nickname> :There was no such nickname
            ErrTooManyTargets = 407,                // <target> :Duplicate recipients. No message delivered
            ErrNoColors = 408,                      // <nickname> #<channel> :You cannot use colors on this channel. Not sent: <text>   
            ErrNoOrigin = 409,                      // :No origin specified
            ErrNoRecipient = 411,                   // :No recipient given (<command>)
            ErrNoTextToSend = 412,                  // :No text to send
            ErrNoTopLevel = 413,                    // <mask> :No toplevel domain specified
            ErrWildTopLevel = 414,                  // <mask> :Wildcard in toplevel Domain
            ErrBadMask = 415, 
            ErrTooMuchInfo = 416,                   // <command> :Too many lines in the output, restrict your query                     
            ErrUnknownCommand = 421,                // <command> :Unknown command
            ErrNoMotd = 422,                        // :MOTD File is missing
            ErrNoAdminInfo = 423,                   // <server> :No administrative info available
            ErrFileError = 424,
            ErrNoNicknameGiven = 431,               // :No nickname given
            ErrErroneusNickname = 432,              // <nickname> :Erroneus Nickname
            ErrNickNameInUse = 433,                 // <nickname> :Nickname is already in use.
            ErrNickCollision = 436,                 // <nickname> :Nickname collision KILL
            ErrUnAvailResource = 437,               // <channel> :Cannot change nickname while banned on channel
            ErrNickTooFast = 438,                   // <nick> :Nick change too fast. Please wait <sec> seconds.                         
            ErrTargetTooFast = 439,                 // <target> :Target change too fast. Please wait <sec> seconds.                     
            ErrUserNotInChannel = 441,              // <nickname> <channel> :They aren't on that channel
            ErrNotOnChannel = 442,                  // <channel> :You're not on that channel
            ErrUserOnChannel = 443,                 // <nickname> <channel> :is already on channel
            ErrNoLogin = 444,
            ErrSummonDisabled = 445,                // :SUMMON has been disabled
            ErrUsersDisabled = 446,                 // :USERS has been disabled
            ErrNotRegistered = 451,                 // <command> :Register first.
            ErrNeedMoreParams = 461,                // <command> :Not enough parameters
            ErrAlreadyRegistered = 462,             // :You may not reregister
            ErrNoPermForHost = 463,
            ErrPasswdMistmatch = 464,
            ErrYoureBannedCreep = 465,
            ErrYouWillBeBanned = 466,
            ErrKeySet = 467,                        // <channel> :Channel key already set
            ErrServerCanChange = 468,               // <channel> :Only servers can change that mode                                     
            ErrChannelIsFull = 471,                 // <channel> :Cannot join channel (+l)
            ErrUnknownMode = 472,                   // <char> :is unknown mode char to me
            ErrInviteOnlyChan = 473,                // <channel> :Cannot join channel (+i)
            ErrBannedFromChan = 474,                // <channel> :Cannot join channel (+b)
            ErrBadChannelKey = 475,                 // <channel> :Cannot join channel (+k)
            ErrBadChanMask = 476,
            ErrNickNotRegistered = 477,             // <channel> :You need a registered nick to join that channel.                      
            ErrBanListFull = 478,                   // <channel> <ban> :Channel ban/ignore list is full
            ErrNoPrivileges = 481,                  // :Permission Denied- You're not an IRC operator
            ErrChanOPrivsNeeded = 482,              // <channel> :You're not channel operator
            ErrCantKillServer = 483,                // :You cant kill a server!
            ErrRestricted = 484,                    // <nick> <channel> :Cannot kill, kick or deop channel service                      
            ErrUniqOPrivsNeeded = 485,              // <channel> :Cannot join channel (reason)
            ErrNoOperHost = 491,                    // :No O-lines for your host
            ErrUModeUnknownFlag = 501,              // :Unknown MODE flag
            ErrUsersDontMatch = 502,                // :Cant change mode for other users
            ErrSilenceListFull = 511                // <mask> :Your silence list is full                                               
        }

        static void Main(string[] args)
        {
            using TcpClient tcpClient = new TcpClient();

            //CheckForErrorCodes();

            ConnectionParameters connectionParameters = new ConnectionParameters();

            connectionParameters.serverIP = "irc.libera.chat";
            connectionParameters.serverPort = 6697;
            connectionParameters.username = "dlIRC_test";
            connectionParameters.password = "";
            connectionParameters.serverChannel = "#linux";


            try
            {
                tcpClient.Connect(connectionParameters.serverIP, connectionParameters.serverPort);
                Console.WriteLine("Connected value is {0}", tcpClient.Connected);

                using SslStream sslStream = new SslStream(tcpClient.GetStream()); Thread.Sleep(500);
                sslStream.AuthenticateAsClient(connectionParameters.serverIP);

                StreamWriter streamWriter = new StreamWriter(sslStream);
                StreamReader streamReader = new StreamReader(sslStream);

                streamWriter.WriteLine($"NICK {connectionParameters.username}\r\n");
                streamWriter.Flush();
                streamWriter.WriteLine($"USER {connectionParameters.username} 8 * :{connectionParameters.username}dlIRC\r\n");
                streamWriter.Flush();

                while (true)
                {
                    try
                    {
                        
                        string ircData = streamReader.ReadLine();
                        Console.WriteLine(ircData);
                        if (ircData != null)
                        {
                            if (ircData.Contains("PING"))
                            {
                                string pingID = ircData.Split(":")[1];
                                streamWriter.WriteLine($"PONG :{pingID}");
                                streamWriter.Flush();
                            }

                            if (CheckForErrorCodes(ircData))
                            {
                                if(ErrorVariables.responseNumber == 266)
                                {
                                    JoinIrcChannel(connectionParameters.serverChannel, connectionParameters.username, streamWriter, streamReader);
                                }
                            }
                        }
                        Thread.Sleep(1);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        //return false;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static bool CheckForErrorCodes(string data)
        {
            string errorMessage;
            string dataString = data.Split(" ")[1].Trim();
            if(int.TryParse(dataString, out ErrorVariables.responseNumber))
            {
                foreach(int errorCode in Enum.GetValues(typeof(ErrorCodes)))
                {
                    if (errorCode == ErrorVariables.responseNumber)
                    {
                        var errorType = Type.GetType($"ErrorCodes.{errorCode.GetType}");
                        errorMessage = Enum.GetName(errorType, errorCode);
                        Console.WriteLine(errorMessage);
                        return false;
                    }
                }

                errorMessage = "No errors found";
                Console.WriteLine(errorMessage);
                return true;
            }
            else
            {
                Console.WriteLine("Couldn't parse number");
                ErrorVariables.responseNumber = 0;
                errorMessage = "Number not parsable; no error code found";
                return true;
            }

        }

        public static bool JoinIrcChannel(string channel, string username, StreamWriter streamWriter, StreamReader streamReader)
        {
            streamWriter.WriteLine($"JOIN {channel}\r\n");
            streamWriter.Flush();
            while (true)
            {
                string ircData = streamReader.ReadLine();
                if(ircData != null)
                {
                    if(ircData.Contains("PING"))
                    {
                        string pingID = ircData.Split(":")[1];
                        streamWriter.WriteLine($"PONG :{pingID}");
                        streamWriter.Flush();
                    }

                    if(ircData.Contains(username) && ircData.Contains("JOIN"))
                    {
                        return true;
                    }
                }
                Thread.Sleep(1);
            }
        }
    }
}