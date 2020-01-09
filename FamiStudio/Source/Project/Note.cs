﻿using System.Diagnostics;

namespace FamiStudio
{
    public struct Note
    {
        public static string[] NoteNames = 
        {
            "C",
            "C#",
            "D",
            "D#",
            "E",
            "F",
            "F#",
            "G",
            "G#",
            "A",
            "A#",
            "B"
        };

        public const int EffectNone  = 0;
        public const int EffectJump  = 1; // Bxx
        public const int EffectSkip  = 2; // Dxx
        public const int EffectSpeed = 3; // Fxx

        public const int VolumeInvalid = 0xff;
        public const int VolumeMax     = 0x0f;

        public const int SlideTypeNone       = 0;
        public const int SlideTypeSlide      = 1;
        public const int SlideTypePortamento = 2;

        public const int FlagsNone     = 0x00;
        public const int FlagsNoAttack = 0x01;

        public const int NoteInvalid    = 0xff;
        public const int NoteStop       = 0x00;
        public const int MusicalNoteMin = 0x01;
        public const int MusicalNoteMax = 0x60;
        public const int NoteRelease    = 0xf7;
        public const int DPCMNoteMin    = 0x0c;
        public const int DPCMNoteMax    = 0x4b;

        public byte Value; // (0 = stop, 1 = C0 ... 96 = B7).
        public byte Effect; // Tempo/Jump/Skip
        public byte EffectParam; // Value for fx.
        public byte Volume; // 0-15. 0xff = no volume change.
        public byte Flags;
        public byte Slide; 
        public Instrument Instrument;

        public Note(int value)
        {
            Value = (byte)value;
            Effect = 0;
            EffectParam = 0;
            Volume = VolumeInvalid;
            Slide = 0;
            Flags = 0;
            Instrument = null;
        }

        public void Clear(bool preserveFx = true)
        {
            Value = NoteInvalid;
            Instrument = null;
            Slide = 0;
            Flags = 0;

            if (!preserveFx)
            {
                Effect = 0;
                EffectParam = 0;
                Volume = VolumeInvalid;
            }
        }

        public bool IsValid
        {
            get { return Value != NoteInvalid; }
            set { if (!value) Value = NoteInvalid; }
        }

        public bool IsStop
        {
            get { return Value == NoteStop; }
            set { if (value) Value = NoteStop; }
        }

        public bool IsRelease
        {
            get { return Value == NoteRelease; }
        }

        public bool IsMusical
        {
            get { return IsValid && !IsStop && !IsRelease; }
        }

        public bool IsSlideNote
        {
            get { return Slide != 0; }
            set { if (!value) Slide = 0; }
        }

        public byte SlideNoteTarget
        {
            get { return Slide; }
            set { Slide = value; }
        }

        public bool HasEffect
        {
            get { return Effect != EffectNone; }
            set { if (!value) Effect = EffectNone; }
        }

        public bool HasVolume
        {
            get { return Volume != VolumeInvalid; }
            set { if (!value) Volume = VolumeInvalid; }
        }

        public bool HasAttack
        {
            get { return (Flags & FlagsNoAttack) == 0; }
            set
            {
                Flags = (byte)(Flags & ~FlagsNoAttack);
                if (!value) Flags = (byte)(Flags | FlagsNoAttack);
            }
        }

        public string FriendlyName
        {
            get
            {
                return GetFriendlyName(Value);
            }
        }

        public static string GetFriendlyName(int value)
        {
            if (value == NoteStop)
                return "Stop Note";
            if (value == NoteRelease)
                return "Release Note";
            if (value == NoteInvalid)
                return "";

            int octave = (value - 1) / 12;
            int note   = (value - 1) % 12;

            return NoteNames[note] + octave.ToString();
        }

        public static int GetEffectMaxValue(Song song, int fx)
        {
            switch (fx)
            {
                case EffectJump  : return song.Length;
                case EffectSkip  : return song.PatternLength;
                case EffectSpeed : return 31;
            }

            return 0;
        }

        public static int Clamp(int note)
        {
            Debug.Assert(note != NoteInvalid);
            if (note < MusicalNoteMin) return MusicalNoteMin;
            if (note > MusicalNoteMax) return MusicalNoteMax;
            return note;
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref Value);
            buffer.Serialize(ref Effect);
            buffer.Serialize(ref EffectParam);

            // At version 3 (FamiStudio 1.2.0), we added a volume track.
            if (buffer.Version >= 3)
                buffer.Serialize(ref Volume);
            else
                Volume = Note.VolumeInvalid;

            // At version 4 (FamiStudio 1.4.0), we added slide notes and no-attack notes.
            if (buffer.Version >= 4)
            {
                buffer.Serialize(ref Flags);
                buffer.Serialize(ref Slide);
            }

            buffer.Serialize(ref Instrument);
        }
    }
}
