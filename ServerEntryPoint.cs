using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace PottyMouth
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private List<EdlSequence> muteList = new List<EdlSequence>();

        private ISessionManager SessionManager { get; set; }

        private IUserManager UserManager { get; set; }

        private IServerConfigurationManager ConfigManager { get; set; }

        private ILogger Log { get; set; }

        private string Locale = string.Empty;

        private GeneralCommand gcMute = new GeneralCommand();
        private GeneralCommand gcUnMute = new GeneralCommand();
       
        public ServerEntryPoint(ISessionManager sessionManager, IUserManager userManager, ILogManager logManager, IServerConfigurationManager configManager)
        {
            SessionManager = sessionManager;
            UserManager = userManager;
            ConfigManager = configManager;
            Log = logManager.GetLogger(Plugin.Instance.Name);

            gcMute.Name = "Mute";
            gcUnMute.Name = "Unmute";
        }


        public void Dispose()
        {
            SessionManager.PlaybackStart -= PlaybackStart;
            SessionManager.PlaybackStopped -= PlaybackStopped;
            SessionManager.PlaybackProgress -= PlaybackProgress;
        }

        public void Run()
        {
            // Set for correct parsing of the EDL file regardless of servers culture
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            // TODO: When Emby adds clients locale to the Session object, use that instead of the servers locale below.
            Locale = ConfigManager.Configuration.UICulture;
            Log.Debug("Locale = " + Locale);

            SessionManager.PlaybackStart += PlaybackStart;
            SessionManager.PlaybackStopped += PlaybackStopped;
            SessionManager.PlaybackProgress += PlaybackProgress;

            Plugin.Instance.UpdateConfiguration(Plugin.Instance.Configuration);
        }

        /// <summary>
        /// Executed on a playback started Emby event. Read the EDL file and add to muteList.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            if (Plugin.Instance.Configuration.EnablePottyMouth == false)
            {
                Log.Debug("PlaybackStart: Plugin is disabled.");
                return;
            }

            string filePath = e.MediaInfo.Path;
            string session = e.Session.Id;
            string edlFile = Path.ChangeExtension(filePath, ".edl");

            if (!File.Exists(edlFile))
            {
                Log.Debug($"PottyMouth EDL file [{edlFile}] does not exist.");
                return;
            }

            //Log.Debug("Session Supported Commands:");
            //foreach(string c in e.Session.Capabilities.SupportedCommands)
            //    Log.Debug(c);

            if (e.Session.Capabilities.SupportedCommands.Contains("Mute"))
            {
                Log.Debug($"Playback Session = {session}  Path = {filePath}");

                if(ReadEdlFile(e) == true)
                    Log.Info($"{filePath} will have its audio censored by request.");
            }
            else
                Log.Info($"Playback Session {session} Path {filePath}. Mute is not supported.  Not possible to mute out the desired audio.");
        }

        /// <summary>
        /// Executed on a playback prorgress Emby event. See if it is in a identified commercial and skip if it is.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            if (Plugin.Instance.Configuration.EnablePottyMouth == false)
                return;

            if (e.Session.PlayState.IsPaused || !e.PlaybackPositionTicks.HasValue)
                return;

            string session = e.Session.Id;         
            long playbackPositionTicks = e.PlaybackPositionTicks.Value;
            
            EdlSequence found = muteList.Find(x => x.sessionId == session && x.processed == false && playbackPositionTicks >= x.startTicks && playbackPositionTicks < (x.endTicks - 1000));
            if (found != null)
            {
                found.processed = true;

                string controlSession = (e.Session.SupportsRemoteControl)
                    ? e.Session.Id
                    : SessionManager.Sessions.Where(i => i.DeviceId == e.Session.DeviceId && i.SupportsRemoteControl).FirstOrDefault().Id;

                if (string.IsNullOrEmpty(controlSession))
                {
                    Log.Debug($"No control session for SessionID {e.Session.Id}");
                    return;
                }
             
                MuteAudio(controlSession, found.endTicks - found.startTicks);

                Log.Debug("Muting audio. Session: " + session + " Start = " + found.startTicks.ToString() + "  End = " + found.endTicks.ToString());
            }
        }

        /// <summary>
        /// Executed on a playback stopped Emby event. Remove the muteList entries for the session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            if (Plugin.Instance.Configuration.EnablePottyMouth == false)
                return;

            string name = e.MediaInfo.Name;
            string sessionID = e.Session.Id;

            // Remove all items in skip list with this session ID
            lock (muteList)
            {
                muteList.RemoveAll(x => x.sessionId == sessionID);
            }

            Log.Debug("Playback Stopped. Session = " + sessionID + " Name = " + name);
        }

        /// <summary>
        /// Read and process the comskip EDL file
        /// </summary>
        /// <param name="e"></param>
        private bool ReadEdlFile(PlaybackProgressEventArgs e)
        {
            string filePath = e.MediaInfo.Path;
            string session = e.Session.Id;

            string edlFile = Path.ChangeExtension(filePath, ".edl");
            Log.Debug("Media File: " + filePath + "   EDL file " + edlFile);

            // Check for edl file and load skip list if found
            // Seconds to ticks = seconds * TimeSpan.TicksPerSecond
            
            if (!File.Exists(edlFile))
            {
                Log.Debug($"PottyMouth EDL file [{edlFile}] does not exist.");
                return false;
            }

            // Remove any stragglers
            lock (muteList)
            {
                muteList.RemoveAll(x => x.sessionId == session);
            }

            Log.Debug($"EDL file {edlFile} found.");

            List<EdlSequence> commTempList = new List<EdlSequence>();

            try
            {
                string line;
                using (StreamReader reader = File.OpenText(edlFile))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line))
                            continue;

                        string[] parts = line.Split('\t');
                        Log.Debug("parts " + parts[0] + " " + parts[1] + " " + parts[2]);

                        // 1 indicates it is meant to mute audio, not skip
                        if (parts[2] == "1")
                        {
                            Log.Debug($"StartOffset = {Plugin.Instance.Configuration.startOffset}");
                            Log.Debug($"EndOffset = {Plugin.Instance.Configuration.endOffset}");

                            EdlSequence seq = new EdlSequence();
                            seq.sessionId = session;
                            seq.startTicks = (long)((double.Parse(parts[0]) - (double)(Plugin.Instance.Configuration.startOffset  / 1000.0)) * (double)TimeSpan.TicksPerSecond);

                            if (seq.startTicks < TimeSpan.TicksPerSecond)
                                seq.startTicks = TimeSpan.TicksPerSecond;

                            seq.endTicks = (long)((double.Parse(parts[1]) + (double)(Plugin.Instance.Configuration.endOffset / 1000.0)) * (double)TimeSpan.TicksPerSecond);

                            Log.Debug($"Final startTicks = {seq.startTicks}");
                            Log.Debug($"Final endTicks = {seq.endTicks}");

                            commTempList.Add(seq);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not parse EDL file " + edlFile + ". Exception: " + ex.Message);
                return false;
            }

            lock (muteList)
            {
                muteList.AddRange(commTempList);
            }

            Log.Debug("PottyMouth List in ticks for " + e.MediaInfo.Name + ":");
            foreach (EdlSequence s in commTempList)
            {
                Log.Debug("Start: " + s.startTicks.ToString() + "  End: " + s.endTicks.ToString());
            }

            return true;
        }


        /// <summary>
        /// Mute the audio for the given session for the given duration
        /// </summary>
        /// <param name="sessionID"></param>
        /// <param name="seek"></param>
        private async void MuteAudio(string sessionID, long timeTicks)
        {
            try
            {
                await SessionManager.SendGeneralCommand(sessionID, sessionID, gcMute, CancellationToken.None).ConfigureAwait(false);

                int sleepTimeSec = (int)Math.Round(((double)timeTicks / (double)TimeSpan.TicksPerSecond), MidpointRounding.AwayFromZero);
                ThreadInfo ti = new ThreadInfo();
                ti.muteTimeSeconds = sleepTimeSec;
                ti.sessionID = sessionID;
                Thread unmuteThread = new Thread(WaitThenUnmute);
                unmuteThread.Start(ti);
            }
            catch //(Exception ex)
            {
         //       Log.Error(ex.Message);
            }
        }

        private void WaitThenUnmute(Object obj)
        {
            ThreadInfo ti = (ThreadInfo)obj;

       //     Log.Debug($"WaitThenUnmute: Sleeping {ti.muteTimeSeconds * 1000} seconds for session {ti.sessionID}");
            Thread.Sleep(ti.muteTimeSeconds * 1000);

            try
            {
                SessionManager.SendGeneralCommand(ti.sessionID, ti.sessionID, gcUnMute, CancellationToken.None);
            }
            catch { }
        }
    }



    /// <summary>
    /// EDL file representation
    /// </summary>
    public class EdlSequence
    {
        public string sessionId { get; set; }
        public bool processed { get; set; } = false;
        public long startTicks { get; set; }
        public long endTicks { get; set; }
    }

    /// <summary>
    /// EDL timestamp
    /// </summary>
    public class EdlTimestamp
    {
        public long timeLoaded { get; set; }
        public string sessionId { get; set; }
    }

    public class ThreadInfo
    {
        public string sessionID { get; set; }
        public int muteTimeSeconds { get; set; }
    }
}
