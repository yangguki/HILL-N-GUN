using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace HillNGun.Editor
{
    public static class AudioGenerator
    {
        private const string OUTPUT_PATH = "Assets/HillNGun/Audio/Generated";
        private const int SAMPLE_RATE = 44100;

        [MenuItem("HillNGun/Audio/Generate Placeholder Audio", false, 51)]
        public static void GenerateAllAudio()
        {
            EnsureDirectories();

            GenerateSFX();
            GenerateBGM();

            AssetDatabase.Refresh();
            Debug.Log("[AudioGenerator] All placeholder audio generated successfully!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new string[]
            {
                OUTPUT_PATH,
                $"{OUTPUT_PATH}/SFX",
                $"{OUTPUT_PATH}/BGM"
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        #region SFX
        private static void GenerateSFX()
        {
            SaveWav("SFX/shot_light", GenerateShotSound(0.08f, 800f, 200f, 0.6f));
            SaveWav("SFX/shot_heavy", GenerateShotSound(0.12f, 400f, 120f, 0.8f));
            SaveWav("SFX/shot_shotgun", GenerateShotgunSound(0.15f));
            SaveWav("SFX/charge_loop", GenerateChargeLoop(1.0f));
            SaveWav("SFX/charge_ready", GeneratePingSound(0.15f, 1800f));

            SaveWav("SFX/reload_click", GenerateClickSound(0.04f, 1200f));
            SaveWav("SFX/reload_full", GenerateReloadFullSound(0.25f));

            SaveWav("SFX/roll", GenerateWhooshSound(0.2f));
            SaveWav("SFX/footstep", GenerateFootstepSound(0.08f));

            SaveWav("SFX/grenade_throw", GenerateThrowSound(0.15f));
            SaveWav("SFX/grenade_explode", GenerateExplosionSound(0.4f));

            SaveWav("SFX/cover_hit", GenerateWoodHitSound(0.1f));
            SaveWav("SFX/cover_break", GenerateWoodBreakSound(0.25f));

            SaveWav("SFX/hit_light", GenerateHitSound(0.06f, 1000f));
            SaveWav("SFX/hit_heavy", GenerateHitSound(0.1f, 600f));
            SaveWav("SFX/downed", GenerateDownedSound(0.4f));
            SaveWav("SFX/revive", GenerateReviveSound(0.5f));
            SaveWav("SFX/skill_activate", GenerateSkillSound(0.2f));

            SaveWav("SFX/boss_roar", GenerateBossRoar(0.8f));
            SaveWav("SFX/boss_warning", GenerateBossWarning(0.6f));
            SaveWav("SFX/boss_phase_change", GeneratePhaseChangeSound(1.0f));

            SaveWav("SFX/ui_click", GenerateUIClick(0.05f));
            SaveWav("SFX/ui_hover", GenerateUIHover(0.03f));
            SaveWav("SFX/ui_back", GenerateUIBack(0.08f));

            SaveWav("SFX/victory_fanfare", GenerateVictoryFanfare(2.0f));
            SaveWav("SFX/defeat_sting", GenerateDefeatSting(1.5f));
        }

        private static float[] GenerateShotSound(float duration, float startFreq, float endFreq, float intensity)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random(42);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float envelope = Mathf.Pow(1f - t, 2f);
                float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.4f;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t * duration);
                samples[i] = (tone * 0.6f + noise) * envelope * intensity;
            }

            return samples;
        }

        private static float[] GenerateShotgunSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random(43);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Pow(1f - t, 1.5f);
                float noise = ((float)rand.NextDouble() * 2f - 1f);
                float lowFreq = Mathf.Sin(2f * Mathf.PI * 150f * t * duration);
                samples[i] = (noise * 0.5f + lowFreq * 0.5f) * envelope * 0.9f;
            }

            return samples;
        }

        private static float[] GenerateChargeLoop(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = 200f + t * 400f;
                float wobble = Mathf.Sin(t * 30f) * 50f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * (freq + wobble) * t * duration) * 0.3f * t;
            }

            return samples;
        }

        private static float[] GeneratePingSound(float duration, float freq)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 15f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.5f;
            }

            return samples;
        }

        private static float[] GenerateClickSound(float duration, float freq)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 40f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.4f;
            }

            return samples;
        }

        private static float[] GenerateReloadFullSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                
                float click1 = t < 0.1f ? Mathf.Sin(2f * Mathf.PI * 1500f * t) * Mathf.Exp(-t * 50f) : 0f;
                float click2 = t > 0.4f && t < 0.5f ? Mathf.Sin(2f * Mathf.PI * 2000f * (t - 0.4f)) * Mathf.Exp(-(t - 0.4f) * 50f) : 0f;
                float clack = t > 0.7f ? Mathf.Sin(2f * Mathf.PI * 800f * (t - 0.7f)) * Mathf.Exp(-(t - 0.7f) * 30f) : 0f;

                samples[i] = (click1 + click2 + clack) * 0.5f;
            }

            return samples;
        }

        private static float[] GenerateWhooshSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random(44);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Sin(t * Mathf.PI);
                float noise = (float)rand.NextDouble() * 2f - 1f;
                float filtered = noise * (0.3f + 0.7f * (1f - Mathf.Abs(t - 0.5f) * 2f));
                samples[i] = filtered * envelope * 0.4f;
            }

            return samples;
        }

        private static float[] GenerateFootstepSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random(45);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 25f);
                float noise = (float)rand.NextDouble() * 2f - 1f;
                float thud = Mathf.Sin(2f * Mathf.PI * 80f * t * duration);
                samples[i] = (noise * 0.3f + thud * 0.7f) * envelope * 0.3f;
            }

            return samples;
        }

        private static float[] GenerateThrowSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random(46);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = t < 0.3f ? t / 0.3f : (1f - t) / 0.7f;
                float noise = (float)rand.NextDouble() * 2f - 1f;
                float freq = 400f + t * 200f;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t * duration);
                samples[i] = (noise * 0.4f + tone * 0.2f) * envelope * 0.3f;
            }

            return samples;
        }

        private static float[] GenerateExplosionSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random(47);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = t < 0.05f ? t / 0.05f : Mathf.Exp(-(t - 0.05f) * 5f);
                float noise = (float)rand.NextDouble() * 2f - 1f;
                float lowFreq = Mathf.Sin(2f * Mathf.PI * 50f * t * duration);
                float midFreq = Mathf.Sin(2f * Mathf.PI * 120f * t * duration);
                samples[i] = (noise * 0.4f + lowFreq * 0.4f + midFreq * 0.2f) * envelope * 0.9f;
            }

            return samples;
        }

        private static float[] GenerateWoodHitSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random(48);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 30f);
                float tone = Mathf.Sin(2f * Mathf.PI * 350f * t * duration);
                float noise = (float)rand.NextDouble() * 2f - 1f;
                samples[i] = (tone * 0.6f + noise * 0.3f) * envelope * 0.5f;
            }

            return samples;
        }

        private static float[] GenerateWoodBreakSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random(49);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 10f);
                float noise = (float)rand.NextDouble() * 2f - 1f;

                int crackleCount = 5;
                float crackle = 0f;
                for (int c = 0; c < crackleCount; c++)
                {
                    float crackT = c * 0.15f;
                    if (t > crackT && t < crackT + 0.05f)
                    {
                        crackle += Mathf.Sin(2f * Mathf.PI * (200f + c * 80f) * (t - crackT)) * Mathf.Exp(-(t - crackT) * 40f);
                    }
                }

                samples[i] = (noise * 0.4f + crackle * 0.6f) * envelope * 0.6f;
            }

            return samples;
        }

        private static float[] GenerateHitSound(float duration, float freq)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 20f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.4f;
            }

            return samples;
        }

        private static float[] GenerateDownedSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(500f, 150f, t);
                float envelope = 1f - t;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.4f;
            }

            return samples;
        }

        private static float[] GenerateReviveSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(300f, 800f, t);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float shimmer = Mathf.Sin(2f * Mathf.PI * freq * 2f * t * duration) * 0.3f;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t * duration) + shimmer) * envelope * 0.35f;
            }

            return samples;
        }

        private static float[] GenerateSkillSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = 800f + Mathf.Sin(t * 40f) * 200f;
                float envelope = t < 0.1f ? t / 0.1f : Mathf.Exp(-(t - 0.1f) * 8f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.4f;
            }

            return samples;
        }

        private static float[] GenerateBossRoar(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];
            System.Random rand = new System.Random(50);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = t < 0.1f ? t / 0.1f : (t < 0.4f ? 1f : (1f - t) / 0.6f);
                float freq = 80f + Mathf.Sin(t * 15f) * 30f;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t * duration);
                float harmonic = Mathf.Sin(2f * Mathf.PI * freq * 2f * t * duration) * 0.3f;
                float noise = (float)rand.NextDouble() * 2f - 1f;
                samples[i] = (tone + harmonic + noise * 0.2f) * envelope * 0.6f;
            }

            return samples;
        }

        private static float[] GenerateBossWarning(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float pulseRate = 8f;
                float envelope = (Mathf.Sin(t * pulseRate * Mathf.PI * 2f) + 1f) * 0.5f;
                float freq = 600f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.5f;
            }

            return samples;
        }

        private static float[] GeneratePhaseChangeSound(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = 200f + t * 600f;
                float envelope = Mathf.Sin(t * Mathf.PI);
                float shimmer = Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t * duration) * 0.2f;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t * duration) + shimmer) * envelope * 0.5f;
            }

            return samples;
        }

        private static float[] GenerateUIClick(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 50f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1000f * t * duration) * envelope * 0.3f;
            }

            return samples;
        }

        private static float[] GenerateUIHover(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 60f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1500f * t * duration) * envelope * 0.15f;
            }

            return samples;
        }

        private static float[] GenerateUIBack(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(800f, 400f, t);
                float envelope = Mathf.Exp(-t * 20f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.3f;
            }

            return samples;
        }

        private static float[] GenerateVictoryFanfare(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            float[] notes = { 392f, 440f, 494f, 523f, 587f, 659f };
            float noteLength = duration / notes.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = Mathf.Min((int)(t / noteLength), notes.Length - 1);
                float noteT = (t - noteIndex * noteLength) / noteLength;
                
                float envelope = noteT < 0.1f ? noteT / 0.1f : Mathf.Exp(-(noteT - 0.1f) * 3f);
                float freq = notes[noteIndex];
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float harmonic = Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.3f;
                
                samples[i] = (tone + harmonic) * envelope * 0.4f;
            }

            return samples;
        }

        private static float[] GenerateDefeatSting(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            float[] notes = { 392f, 349f, 311f, 277f };
            float noteLength = duration / notes.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = Mathf.Min((int)(t / noteLength), notes.Length - 1);
                float noteT = (t - noteIndex * noteLength) / noteLength;
                
                float envelope = Mathf.Exp(-noteT * 2f);
                float freq = notes[noteIndex];
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                
                samples[i] = tone * envelope * 0.35f;
            }

            return samples;
        }
        #endregion

        #region BGM
        private static void GenerateBGM()
        {
            SaveWav("BGM/bgm_menu", GenerateMenuBGM(30f));
            SaveWav("BGM/bgm_battle", GenerateBattleBGM(30f));
            SaveWav("BGM/bgm_boss", GenerateBossBGM(30f));
        }

        private static float[] GenerateMenuBGM(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            float bpm = 90f;
            float beatLength = 60f / bpm;
            
            float[] bassNotes = { 110f, 110f, 146.83f, 130.81f };
            float[] melodyNotes = { 440f, 493.88f, 523.25f, 587.33f, 523.25f, 493.88f, 440f, 392f };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float loopT = t % (beatLength * 8);
                
                int bassIndex = (int)(loopT / (beatLength * 2)) % bassNotes.Length;
                float bassFreq = bassNotes[bassIndex];
                float bassEnv = Mathf.Exp(-((loopT % (beatLength * 2)) / (beatLength * 2)) * 2f);
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * bassEnv * 0.3f;

                int melodyIndex = (int)(loopT / beatLength) % melodyNotes.Length;
                float melodyFreq = melodyNotes[melodyIndex];
                float melodyT = (loopT % beatLength) / beatLength;
                float melodyEnv = melodyT < 0.1f ? melodyT / 0.1f : Mathf.Exp(-(melodyT - 0.1f) * 4f);
                float melody = Mathf.Sin(2f * Mathf.PI * melodyFreq * t) * melodyEnv * 0.25f;
                float melodyHarmonic = Mathf.Sin(2f * Mathf.PI * melodyFreq * 2f * t) * melodyEnv * 0.1f;

                samples[i] = bass + melody + melodyHarmonic;
            }

            return samples;
        }

        private static float[] GenerateBattleBGM(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            float bpm = 120f;
            float beatLength = 60f / bpm;

            float[] bassNotes = { 82.41f, 82.41f, 110f, 98f };
            float[] leadNotes = { 329.63f, 392f, 440f, 392f, 329.63f, 293.66f, 329.63f, 349.23f };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float loopT = t % (beatLength * 8);

                float kickT = loopT % beatLength;
                float kick = kickT < 0.1f ? Mathf.Sin(2f * Mathf.PI * (150f - kickT * 800f) * kickT) * Mathf.Exp(-kickT * 30f) * 0.4f : 0f;

                float snareT = (loopT + beatLength / 2) % beatLength;
                float snare = 0f;
                if ((int)(loopT / beatLength) % 2 == 1 && snareT < 0.08f)
                {
                    System.Random rand = new System.Random((int)(t * 1000) % 1000);
                    snare = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-snareT * 40f) * 0.25f;
                }

                int bassIndex = (int)(loopT / (beatLength * 2)) % bassNotes.Length;
                float bassFreq = bassNotes[bassIndex];
                float bassEnv = 0.5f + 0.5f * Mathf.Cos((loopT % (beatLength * 2)) / (beatLength * 2) * Mathf.PI);
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * bassEnv * 0.25f;

                int leadIndex = (int)(loopT / beatLength) % leadNotes.Length;
                float leadFreq = leadNotes[leadIndex];
                float leadT = (loopT % beatLength) / beatLength;
                float leadEnv = leadT < 0.05f ? leadT / 0.05f : Mathf.Exp(-(leadT - 0.05f) * 5f);
                float vibrato = Mathf.Sin(t * 30f) * 5f;
                float lead = Mathf.Sin(2f * Mathf.PI * (leadFreq + vibrato) * t) * leadEnv * 0.2f;

                samples[i] = Mathf.Clamp(kick + snare + bass + lead, -1f, 1f);
            }

            return samples;
        }

        private static float[] GenerateBossBGM(float duration)
        {
            int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[sampleCount];

            float bpm = 140f;
            float beatLength = 60f / bpm;

            float[] bassNotes = { 73.42f, 73.42f, 82.41f, 77.78f };
            float[] leadNotes = { 293.66f, 349.23f, 392f, 349.23f, 293.66f, 261.63f, 293.66f, 311.13f };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float loopT = t % (beatLength * 8);

                float kickT = loopT % (beatLength / 2);
                float kick = kickT < 0.05f ? Mathf.Sin(2f * Mathf.PI * (200f - kickT * 1500f) * kickT) * Mathf.Exp(-kickT * 50f) * 0.35f : 0f;

                int bassIndex = (int)(loopT / (beatLength * 2)) % bassNotes.Length;
                float bassFreq = bassNotes[bassIndex];
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.3f;
                float bassOctave = Mathf.Sin(2f * Mathf.PI * bassFreq * 2f * t) * 0.15f;

                int leadIndex = (int)(loopT / beatLength) % leadNotes.Length;
                float leadFreq = leadNotes[leadIndex];
                float leadT = (loopT % beatLength) / beatLength;
                float leadEnv = leadT < 0.03f ? leadT / 0.03f : Mathf.Exp(-(leadT - 0.03f) * 6f);
                float vibrato = Mathf.Sin(t * 40f) * 8f;
                float lead = Mathf.Sin(2f * Mathf.PI * (leadFreq + vibrato) * t) * leadEnv * 0.25f;

                float tension = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.1f;

                samples[i] = Mathf.Clamp(kick + bass + bassOctave + lead + tension, -1f, 1f);
            }

            return samples;
        }
        #endregion

        #region WAV Writing
        private static void SaveWav(string relativePath, float[] samples)
        {
            string fullPath = $"{OUTPUT_PATH}/{relativePath}.wav";
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            WriteWav(fullPath, samples, SAMPLE_RATE);

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(fullPath) as AudioImporter;
            if (importer != null)
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static void WriteWav(string path, float[] samples, int sampleRate)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int channels = 1;
                int bitsPerSample = 16;
                int byteRate = sampleRate * channels * bitsPerSample / 8;
                int blockAlign = channels * bitsPerSample / 8;
                int dataSize = samples.Length * blockAlign;

                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)blockAlign);
                writer.Write((short)bitsPerSample);

                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                foreach (float sample in samples)
                {
                    short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767);
                    writer.Write(intSample);
                }
            }
        }
        #endregion
    }
}
