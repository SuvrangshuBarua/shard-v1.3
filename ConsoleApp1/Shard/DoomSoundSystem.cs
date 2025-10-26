using SDL2;
using System;
using System.Collections.Generic;
using static SDL2.SDL_mixer;

namespace Shard.Shard
{
    internal class DoomSoundSystem : Sound
    {
        private nint music;
        private float volume = 1.0f;
        private float previousVolume = 0.5f; // Store previous volume for unmute
        public bool isMuted = false; // Track mute state
        public DateTime muteIndicatorStartTime; // Tracks when mute was toggled
        public bool showMuteIndicator; // Controls indicator visibility
        private string muteIndicatorText = "";
        public bool isLoop = false;

        private Dictionary<string, nint> bgmTrack = new Dictionary<string, nint>(); // Store sound
        private Dictionary<string, nint> soundEffects = new Dictionary<string, nint>(); // Store SFX
        private Dictionary<string, int> soundChannels = new Dictionary<string, int>();

        public override void Initialize()
        {
            // Set the audio driver (e.g., "directsound" for Windows)
            Environment.SetEnvironmentVariable("SDL_AUDIODRIVER", "directsound");
            // Initialize SDL2 audio subsystem
            if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) < 0)
            {
                Console.WriteLine("Failed to initialize audio: " + SDL.SDL_GetError());
                return;
            }
            // Initialize SDL2_mixer
            if (Mix_OpenAudio(44100, MIX_DEFAULT_FORMAT, 2, 2048) < 0)
            {
                Console.WriteLine("Failed to initialize SDL2_mixer: " + SDL.SDL_GetError());
                return;
            }
            // Allocate channels for simultaneous sounds (footsteps, shooting, etc.)
            Mix_AllocateChannels(100);  // Ensures up to 100 sounds can play at once

            Console.WriteLine("SDL2_mixer Initialized with 100 channels.");
            Console.WriteLine("SDL2_mixer Initialized.");
        }

        public void isLooping(bool isLoop, string file)
        {
            this.isLoop = isLoop;
            playSound(file);
        }

        public override void playSound(string file)
        {
            // TODO: Use the AssetManager to get the relative file path?
            file = "../../../../Assets/" + file;

            if (isLoop) // Handle looping sounds (BGM)
            {
                Console.WriteLine("Loading looping sound from path: " + file);
                nint bgmTrack = Mix_LoadMUS(file); // Load a new music track

                if (bgmTrack == nint.Zero)
                {
                    Console.WriteLine("Failed to load looping sound: " + SDL.SDL_GetError());
                    return;
                }

                if (Mix_PlayMusic(bgmTrack, -1) == -1) // Loop indefinitely
                {
                    Console.WriteLine("Failed to play looping sound: " + SDL.SDL_GetError());
                    return;
                }
                SetVolume(volume);
                //Console.WriteLine("Playing background music...");
            }
            else
            {
                // Check if sound effect is already loaded
                if (!soundEffects.ContainsKey(file))
                {
                    nint sfx = Mix_LoadWAV(file);
                    if (sfx == nint.Zero)
                    {
                        Console.WriteLine("Failed to load sound effect: " + SDL.SDL_GetError());
                        return;
                    }
                    soundEffects[file] = sfx; // Store in dictionary
                }

                int channel;

                // Check if the sound already has an assigned channel
                if (soundChannels.ContainsKey(file))
                {
                    channel = soundChannels[file]; // Reuse the same channel
                }
                else
                {
                    channel = Mix_GroupAvailable(-1); // Get an available channel dynamically
                    if (channel == -1)
                    {
                        Console.WriteLine("No available channels! Playing on a random channel.");
                        channel = Mix_PlayChannel(-1, soundEffects[file], 0); // Play on any free channel
                        return;
                    }
                    soundChannels[file] = channel; // Store assigned channel for future use
                }
                // **Set a lower volume for sound effects (e.g., 50% of max)**
                Mix_VolumeChunk(soundEffects[file], 64); // 128 = max, 64 = half volume
                // Play the sound effect without stopping BGM
                Mix_HaltChannel(channel);
                // Play the sound effect on its assigned channel
                Mix_PlayChannel(channel, soundEffects[file], 0);
            }
        }

        public override void Stop()
        {
            // Stop the currently playing music
            if (Mix_PlayingMusic() == 1)
            {
                Mix_HaltMusic();
                Console.WriteLine("Music stopped.");
            }

            // Free the music if it's loaded
            if (music != nint.Zero)
            {
                Mix_FreeMusic(music);
                music = nint.Zero;
            }
        }

        public override void SetVolume(float volume)
        {
            // Clamp volume between 0 (mute) and 1 (max)
            this.volume = Math.Max(0.0f, Math.Min(1.0f, volume));

            // Convert to SDL2_mixer's scale (0 to 128)
            int sdlVolume = (int)(this.volume * 128);

            // Set volume for BGM
            Mix_VolumeMusic(sdlVolume);

            // Set volume for all sound effects
            foreach (var sfx in soundEffects.Values)
            {
                Mix_VolumeChunk(sfx, sdlVolume);
            }

            Console.WriteLine($"Global Volume Set to: {sdlVolume}");
        }

        public void ToggleMute()
        {
            if (isMuted)
            {
                // Unmute: Restore previous volume
                SetVolume(previousVolume);
                isMuted = false;
            }
            else
            {
                // Mute: Store the current volume and mute everything
                previousVolume = volume;
                SetVolume(0.0f);
                isMuted = true;
            }

            showMuteIndicator = isMuted;
            muteIndicatorStartTime = DateTime.Now;
            Console.WriteLine($"Mute State: {isMuted}");
        }

        public void IncreaseVolume(float increment = 0.1f)
        {
            if (!isMuted) // Prevent volume increase while muted
            {
                SetVolume(volume + increment);
            }
        }

        public void DecreaseVolume(float decrement = 0.1f)
        {
            if (!isMuted) // Prevent volume increase while muted
            {
                SetVolume(volume - decrement);
            }
        }

        public override void Cleanup()
        {
            // Stop and free any loaded music
            Stop();

            // Close SDL2_mixer
            Mix_CloseAudio();

            // Shutdown SDL2 audio subsystem
            SDL.SDL_QuitSubSystem(SDL.SDL_INIT_AUDIO);

            Console.WriteLine("Audio system cleaned up.");
        }
    }
}