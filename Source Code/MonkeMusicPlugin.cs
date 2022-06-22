using BepInEx;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using Utilla;

// I've decided to put all the classes in one script, why exactly I have no idea.

namespace MonkeMusic
{
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]

    public class Plugin : BaseUnityPlugin
    {
        // music stuff //
        // made some mistakes here dont mind them lmao //
        readonly List<int> MusicType = new List<int>(); // type of audio file (0=mp3, 1=ogg, 2=wav)
        readonly List<string> Music = new List<string>(); // a list full of the song names
        public static bool LeftScrollCooldown = false; // should the functions for scrolling stop functioning (up and down)
        public static bool LeftScrollCooldown2 = false; // should the functions for scrolling stop functioning (left and right)
        static bool LeftSecondaryCooldown = false; // should the functions for pressing A (oculus touch) stop functioning
        static bool LeftPrimaryCooldown = false; // should the functions for pressing B (oculus touch) stop functioning
        public static bool LeftScollDown = false; // is the player scrolling either up or down
        static bool LeftSecondaryDown = false; // is the player holding A (oculus touch)
        static bool LeftPrimaryDown = false; // is the player holding B (oculus touch)
        public static bool LeftScollDown2 = false; // is the player scrolling either left or right
        public static int CurrentSong = 0; // the current song 
        public static bool paused = false; // is the song paused
        public static AudioSource Source; // the audio source where the music plays
        public static AudioClip Clip; // the audio clip of the music
        public static bool left = true; // is the player scrolling left
        public static bool up = true; // is the player scrolling up
        public string musicpath; // the path for the music folder
        bool update = false; // can update
        Vector2 scrollAxis; // the vector2 scroll axis
        bool ready = true; // can the music be played

        // wrist info stuff //
        Text fileNameText; // the text with the music file name
        Text fileLengthText; // the text with how far you're into the music and how long the music is

        void Awake() // when the script instance is being loaded
        {
            Events.GameInitialized += OnGameInitialized;
        }

        void OnEnable() // when the object is both active and enabled
        {
            ready = true;
            PlaySong();
        }

        void OnDisable() // when the behavior becomes disabled or inactive
        {
            ready = false;
            PlaySong();
        }

        void OnGameInitialized(object sender, EventArgs e) // when the game initialized (requires utilla)
        {
            update = false;
            CreateWristInfo(); // adds the text for the song info
            LoadSongs(); // loads the songs (creates the directory if missing, and puts all the songs into a list)
            CreateSource(); // creates the gameobject with an audiosource where the music is played
            PlaySong(); // plays the CurrentSong taken from that list i mentioned 2 lines up
            if (Music.Count != 0)
            {
                update = true;
            }
        }

        void CreateWristInfo() // creates the info thing on your wrist
        {
            Font Utopium_OL = GameObject.Find("OfflineVRRig/Actual Gorilla/rig/body/Canvas/Text/").GetComponent<Text>().font;

            GameObject UserInterface = new GameObject();
            UserInterface.transform.SetParent(GameObject.Find("OfflineVRRig/Actual Gorilla/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L/").transform, false);
            UserInterface.name = "HandUI";
            UserInterface.transform.localPosition = new Vector3(0.0221f, 0.0773f, 0.0079f);
            UserInterface.transform.localRotation = Quaternion.Euler(15.998f, -97.894f, 84.053f);
            UserInterface.transform.localScale = new Vector3(0.001003236f, 0.001003236f, 0.001003236f);

            fileNameText = CreateText(UserInterface); // 1 creates canvas and text, 2 creates just text
            fileNameText.gameObject.name = "FileName";
            fileNameText.text = "LOADING SONG"; // lowercase utopium is cursed
            fileNameText.alignment = TextAnchor.MiddleCenter;
            fileNameText.font = Utopium_OL;
            fileNameText.gameObject.GetComponent<RectTransform>().transform.localPosition = new Vector3(0, 8.799999f, 0);
            fileNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            fileNameText.verticalOverflow = VerticalWrapMode.Overflow;

            GameObject UserInterface2 = new GameObject();
            UserInterface2.transform.SetParent(GameObject.Find("OfflineVRRig/Actual Gorilla/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L/").transform, false);
            UserInterface2.name = "HandUI2";
            UserInterface2.transform.localPosition = new Vector3(0.0221f, 0.0773f, 0.0079f);
            UserInterface2.transform.localRotation = Quaternion.Euler(15.998f, -97.894f, 84.053f);
            UserInterface2.transform.localScale = new Vector3(0.001003236f, 0.001003236f, 0.001003236f);

            fileLengthText = CreateText(UserInterface2); // 1 creates canvas and text, 2 creates just text
            fileLengthText.gameObject.name = "SongLength";
            fileLengthText.text = "0.00/0.00"; // lowercase utopium is cursed
            fileLengthText.alignment = TextAnchor.MiddleCenter;
            fileLengthText.font = Utopium_OL;
            fileLengthText.gameObject.GetComponent<RectTransform>().transform.localPosition = new Vector3(0, -3f, 0);
            fileLengthText.horizontalOverflow = HorizontalWrapMode.Overflow;
            fileLengthText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        Text CreateText(GameObject obj) // used for creating new gameobjects with text with ease
        {
            obj.AddComponent<Canvas>();
            obj.AddComponent<CanvasRenderer>();

            GameObject gameTheObject2 = new GameObject();
            gameTheObject2.transform.SetParent(obj.transform, false);
            Text text = gameTheObject2.AddComponent<Text>();
            return text;
        }

        void Update() // called every fixed frame if the monobehavior is enabled
        {
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out scrollAxis);
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out LeftSecondaryDown);
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out LeftPrimaryDown);

            if (update && Music.Count != 0) // fix for errors
            {
                if (!paused)
                {
                    fileNameText.text = Music[CurrentSong];
                    fileLengthText.text = $"{(Source.time):F2} / {(Clip.length):F2}";
                    fileNameText.color = Color.white;
                    fileLengthText.color = Color.white;
                }
                else
                {
                    fileNameText.color = new Color(1, 0.111f, 0.111f);
                    fileLengthText.color = new Color(1, 0.111f, 0.111f);
                }

                if (LeftSecondaryDown && !LeftSecondaryCooldown)
                {
                    GorillaTagger.Instance.StartVibration(true, 0.15f, 0.05f);
                    LeftSecondaryCooldown = true;
                    if (Source.isPlaying)
                    {
                        Source.Pause();
                        paused = true;
                    }
                    else
                    {
                        Source.UnPause();
                        paused = false;
                    }
                }
                else if (!LeftSecondaryDown && LeftSecondaryCooldown)
                {
                    LeftSecondaryCooldown = false;
                }

                ///////////////////////////////////

                if (LeftPrimaryDown && !LeftPrimaryCooldown)
                {
                    if (Source.isPlaying && !paused)
                    {
                        GorillaTagger.Instance.StartVibration(true, 0.15f, 0.05f);
                        LeftPrimaryCooldown = true;
                        Source.time = 0;
                    }
                }
                else if (!LeftPrimaryDown && LeftPrimaryCooldown)
                {
                    LeftPrimaryCooldown = false;
                }

                if (scrollAxis.x == 0)
                {
                    LeftScollDown = false;
                }
                else if (scrollAxis.x > 0.785)
                {
                    LeftScollDown = true;
                    up = true;
                }
                else if (scrollAxis.x < -0.785)
                {
                    LeftScollDown = true;
                    up = false;
                }

                if (scrollAxis.y == 0)
                {
                    LeftScollDown2 = false;
                }
                else if (scrollAxis.y > 0.785)
                {
                    LeftScollDown2 = true;
                    left = false;
                }
                else if (scrollAxis.y < -0.785)
                {
                    LeftScollDown2 = true;
                    left = true;
                }

                if (LeftScollDown && !LeftScrollCooldown && !paused)
                {
                    LeftScrollCooldown = true;
                    GorillaTagger.Instance.StartVibration(true, 0.025f, 0.05f);
                    if (!up)
                    {
                        if (CurrentSong < Music.Count - 1)
                        {
                            CurrentSong++;
                        }
                        else
                        {
                            CurrentSong = 0;
                        }
                        PlaySong();
                    }
                    else
                    {
                        //CurrentSong--;
                        if (CurrentSong > 0)
                        {
                            CurrentSong--;
                        }
                        else
                        {
                            CurrentSong = Music.Count - 1;
                        }
                        PlaySong();
                    }
                }
                else if (!LeftScollDown && LeftScrollCooldown)
                {
                    LeftScrollCooldown = false;
                }

                /////////////////////////////////////////////////////////////////////////////

                if (LeftScollDown2 && !LeftScrollCooldown2 && !paused && Source.isPlaying)
                {
                    LeftScrollCooldown2 = true;
                    GorillaTagger.Instance.StartVibration(true, 0.025f, 0.05f);
                    if (!left)
                    {
                        if (Source.time > Clip.length)
                        {
                            if (CurrentSong < Music.Count - 1)
                            {
                                CurrentSong++;
                            }
                            else
                            {
                                CurrentSong = 0;
                            }
                            Source.Stop();

                        }
                        else
                        {
                            Source.time += 10;
                        }

                        if (Source.time > Clip.length)
                        {
                            if (CurrentSong < Music.Count - 1)
                            {
                                CurrentSong++;
                            }
                            else
                            {
                                CurrentSong = 0;
                            }
                            Source.Stop();

                        }
                    }
                    else
                    {
                        if (Source.time < 0)
                        {
                            if (CurrentSong > 0)
                            {
                                CurrentSong--;
                            }
                            else
                            {
                                CurrentSong = Music.Count - 1;
                            }
                            Source.Stop();

                        }
                        else
                        {
                            Source.time -= 10;
                        }
                        if (Source.time < 0)
                        {
                            if (CurrentSong > 0)
                            {
                                CurrentSong--;
                            }
                            else
                            {
                                CurrentSong = Music.Count - 1;
                            }
                            Source.Stop();
                            
                        }

                    }

                    if (!Source.isPlaying)
                    {
                        PlaySong();
                    }
                }
                else if (!LeftScollDown2 && LeftScrollCooldown2)
                {
                    LeftScrollCooldown2 = false;
                }

                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    if (CurrentSong > 0)
                    {
                        CurrentSong--;
                    }
                    else
                    {
                        CurrentSong = Music.Count - 1;
                    }
                    PlaySong();
                }

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    if (CurrentSong < Music.Count - 1)
                    {
                        CurrentSong++;
                    }
                    else
                    {
                        CurrentSong = 0;
                    }
                    PlaySong();
                }

                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    if (Source.isPlaying)
                    {
                        Source.Pause();
                        paused = true;
                    }
                    else
                    {
                        Source.UnPause();
                        paused = false;
                    }
                }
            }
            else if (Music.Count == 0)
            {
                //bruh install some music monkey!!!
            }
        }

        void LoadSongs()
        {
            musicpath = Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "Plugins", PluginInfo.Name.ToString(), "Music");
            if (!Directory.Exists(musicpath))
            {
                Debug.LogError("No music folder was found, creating music folder.");
                Directory.CreateDirectory(musicpath);
            }
            DirectoryInfo directory = new DirectoryInfo(musicpath);
            foreach (var audioFile in directory.GetFiles("*.ogg"))
            {
                MusicType.Add(0);
                Music.Add(audioFile.Name);
            }
            foreach (var audioFile in directory.GetFiles("*.wav"))
            {
                MusicType.Add(1);
                Music.Add(audioFile.Name);
            }
            if (Music.Count == 0)
            {
                Debug.LogError($"No music found in your music folder, please put some .wav files into the folder:\n{musicpath}");
            }
        }

        void CreateSource()
        {
            GameObject obj = new GameObject();
            Source = obj.AddComponent<AudioSource>();
            obj.GetComponent<AudioSource>().enabled = true;
            obj.GetComponent<AudioSource>().volume = 0.085f; // fix audio being very loud for about a fifth of a second
            obj.GetComponent<AudioSource>().playOnAwake = false;
            obj.GetComponent<AudioSource>().loop = true;
        }

        async void PlaySong()
        {
            if (!ready) return; // if the mod is disabled, don't play any music and return the function

            var song = new FileInfo(musicpath + "\\" + Music[CurrentSong]);
            Clip = await SoundLoader.LoadClip(song.FullName, MusicType[CurrentSong]);

            if (Clip == null) return;

            Source.clip = Clip;
            Source.Play();
            Source.volume = 0.085f; // sometimes the volume would return back to 1 without this line

            Source.time = 0;
        }
    } // heres where most of the mod is
    class PluginInfo
    {
        public const string GUID = "com.developer9998.gorillatag.monkemusic";
        public const string Name = "MonkeMusic";
        public const string Version = "1.0.0";
    } // heres the info that bepinex recieves 
    public class SoundLoader
    {
        // Code by ToniMacaroni, this wouldn't be possible without their work: https://github.com/ToniMacaroni/MovementSound
        public static async Task<AudioClip> LoadClip(string path, int musicType)
        {

            if (musicType == 0)
            {
                UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.OGGVORBIS);
                var tcs = new TaskCompletionSource<bool>();
                var op = request.SendWebRequest();
                op.completed += ao => { tcs.SetResult(true); };

                try
                {
                    await tcs.Task;

                    if (request.isNetworkError || request.isHttpError) Debug.LogError($"{request.error}");
                    else
                    {
                        return DownloadHandlerAudioClip.GetContent(request);
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError($"{err.Message}, {err.StackTrace}");
                }

                return null;
            }
            else if (musicType == 1)
            {
                UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);
                var tcs = new TaskCompletionSource<bool>();
                var op = request.SendWebRequest();
                op.completed += ao => { tcs.SetResult(true); };

                try
                {
                    await tcs.Task;

                    if (request.isNetworkError || request.isHttpError) Debug.LogError($"{request.error}");
                    else
                    {
                        return DownloadHandlerAudioClip.GetContent(request);
                    }
                }
                catch (Exception err)
                {
                    Debug.LogError($"{err.Message}, {err.StackTrace}");
                }

                return null;
            }
            else
                return null;
        }
    } // heres where the .wav files gets loaded (credit in class)
}