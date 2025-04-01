using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class AlbuquerqueScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public Sprite[] AllSprites;
    public SpriteRenderer TV;
    public KMSelectable TVSelectable;
    public SpriteRenderer PlayButton;
    public Sprite ReplaySprite;
    public KMSelectable Button;

    public MeshRenderer GreenLED;
    public MeshRenderer RedLED;
    public SpriteRenderer GreenLEDGlow;
    public SpriteRenderer RedLEDGlow;
    public Material[] LEDMats;

    private KMAudio.KMAudioRef Sound;

    private int CurrentStage, CurrentPhrase;
    private List<int> ExpectedInput = new List<int>();
    private List<List<DonutSound>> AllTriplets = new List<List<DonutSound>>();
    private bool PlayingSounds, SkipRequested, EditSkipRequested, CannotPress, TPAutoSkip, TPStruck;

    private Coroutine ButtonAnimCoroutine;

    private class DonutSound
    {
        private string Name;
        private float Length;

        public DonutSound(string name, float length)
        {
            Name = name;
            Length = length;
        }

        public string GetName()
        {
            return Name;
        }

        public float GetLength()
        {
            return Length;
        }
    }

    private class Coordinate
    {
        public int X;
        public int Y;

        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Coordinate GenerateFromPos(int pos)
        {
            return new Coordinate(pos % 6, pos / 6);
        }

        public bool Equals(Coordinate comparison)
        {
            return comparison.X == X && comparison.Y == Y;
        }

        public override string ToString()
        {
            return "(" + X.ToString() + ", " + Y.ToString() + ")";
        }
    }

    private DonutSound[] AllSounds = new DonutSound[] { new DonutSound("intro", 10.184f),
                                                        new DonutSound("question 1", 2.58f), new DonutSound("answer 1", 2.58f),
                                                        new DonutSound("question 2", 2.58f), new DonutSound("answer 2", 2.58f),
                                                        new DonutSound("question 3", 2.58f), new DonutSound("answer 3", 3.066f),
                                                        new DonutSound("question 4", 2.09f), new DonutSound("answer 4", 2.58f),
                                                        new DonutSound("question 5", 2.58f), new DonutSound("answer 5", 2.58f),
                                                        new DonutSound("question 6", 2.58f), new DonutSound("answer 6", 2.58f),
                                                        new DonutSound("outro", 2.947f),
                                                        new DonutSound("strike", 2.239f), new DonutSound("solve", 9.090f) };

    private List<DonutSound> ChosenSounds;

    private enum SoundType
    {
        Intro,
        Question1,
        Answer1,
        Question2,
        Answer2,
        Question3,
        Answer3,
        Question4,
        Answer4,
        Question5,
        Answer5,
        Question6,
        Answer6,
        Outro,
        Strike,
        Solve
    }

    /*private bool[,] PlotGrid = new bool[,]
    {
        { false, false, true, false, false, false },
        { false, false, false, false, false, true },
        { false, false, false, true, false, false },
        { false, true, false, false, false, false },
        { false, false, false, false, true, false },
        { true, false, false, false, false, false }
    };

    private Coordinate[] MarkedCoords = new Coordinate[]
    {
        new Coordinate(0, 2),
        new Coordinate(1, 5),
        new Coordinate(2, 3),
        new Coordinate(3, 1),
        new Coordinate(4, 4),
        new Coordinate(5, 0)
    };*/

    private Coordinate[] MarkedCoords = new Coordinate[]
    {
        new Coordinate(1, 1),
        new Coordinate(1, 4),
        new Coordinate(3, 1),
        new Coordinate(3, 4),
        new Coordinate(4, 2),
        new Coordinate(4, 3)
    };

    private bool MarkedCoordsContains(Coordinate coord)
    {
        foreach (var markedCoord in MarkedCoords)
            if (markedCoord.Equals(coord)) return true;
        return false;
    }

    private int FindEdgeworkCalculation(int trinum, bool isN)
    {
        switch (trinum)
        {
            case 0:
                if (!isN) return Bomb.GetBatteryCount() + Bomb.GetSerialNumberNumbers().Last();
                else return Bomb.GetOffIndicators().Count() + Bomb.GetPortPlateCount();
            case 1:
                if (!isN) return Bomb.GetSerialNumberNumbers().Last() + Bomb.GetPortCount();
                else return Bomb.GetPortPlateCount() + Bomb.GetSerialNumberNumbers().First();
            default:
                if (!isN) return Bomb.GetPortCount() + Bomb.GetOffIndicators().Count();
                else return Bomb.GetSerialNumberNumbers().First() + Bomb.GetBatteryCount();
        }
    }

    private Sprite FindSprite(string name)
    {
        var results = AllSprites.Where(x => x.name == name);
        if (results.Count() > 0)
            return results.First();
        return null;
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        /*ChosenSounds = new List<DonutSound>() { AllSounds[(int)SoundType.Intro],
        AllSounds[(int)SoundType.Question1], AllSounds[(int)SoundType.Answer1],
        AllSounds[(int)SoundType.Question2], AllSounds[(int)SoundType.Answer2],
        AllSounds[(int)SoundType.Question3], AllSounds[(int)SoundType.Answer3],
        AllSounds[(int)SoundType.Question4], AllSounds[(int)SoundType.Answer4],
        AllSounds[(int)SoundType.Question5], AllSounds[(int)SoundType.Answer5],
        AllSounds[(int)SoundType.Question6], AllSounds[(int)SoundType.Answer6],
        AllSounds[(int)SoundType.Question1], AllSounds[(int)SoundType.Answer1],
        AllSounds[(int)SoundType.Question2], AllSounds[(int)SoundType.Answer3],
        AllSounds[(int)SoundType.Question3], AllSounds[(int)SoundType.Answer6],
        AllSounds[(int)SoundType.Question4], AllSounds[(int)SoundType.Answer4],
        AllSounds[(int)SoundType.Question6], AllSounds[(int)SoundType.Answer2],
        AllSounds[(int)SoundType.Question5], AllSounds[(int)SoundType.Answer5],
        AllSounds[(int)SoundType.Question5], AllSounds[(int)SoundType.Answer5],
        AllSounds[(int)SoundType.Question5], AllSounds[(int)SoundType.Answer5],
        AllSounds[(int)SoundType.Question5], AllSounds[(int)SoundType.Outro]
        };

        ChosenSounds = new List<DonutSound>() { AllSounds[(int)SoundType.Intro],
        AllSounds[(int)SoundType.Question2], AllSounds[(int)SoundType.Answer5],
        AllSounds[(int)SoundType.Question3], AllSounds[(int)SoundType.Answer6],
        AllSounds[(int)SoundType.Question1], AllSounds[(int)SoundType.Answer4]
        };*/

        TVSelectable.OnInteract += delegate { TVPress(); return false; };

        StopPlaying();

        Button.OnInteract += delegate { ButtonPress(); return false; };
    }

    // Use this for initialization
    void Start()
    {
        Calculate();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private bool AreCoordinatesAdjacent(Coordinate coord1, Coordinate coord2)
    {
        var xDiff = Mathf.Abs(coord1.X - coord2.X);
        var yDiff = Mathf.Abs(coord1.Y - coord2.Y);
        return xDiff < 2 && yDiff < 2;
    }

    private int AreCoordinatesValidMarked(Coordinate coord1, Coordinate coord2, Coordinate coord3)  // -1 means not valid. 0-2 means "this is the marked cell".
    {
        var markCount = 0;
        var seenMark = 0;

        if (MarkedCoordsContains(coord1)) { markCount++; seenMark = 0; }
        if (MarkedCoordsContains(coord2)) { markCount++; seenMark = 1; }
        if (MarkedCoordsContains(coord3)) { markCount++; seenMark = 2; }

        if (markCount != 1) return -1;

        var adjCount = 0;
        if (AreCoordinatesAdjacent(coord1, coord2)) adjCount++;
        if (AreCoordinatesAdjacent(coord1, coord3)) adjCount++;
        if (AreCoordinatesAdjacent(coord2, coord3)) adjCount++;

        return adjCount < 2 ? seenMark : -1;
    }

    private int AreCoordinatesValidUnmarked(Coordinate coord1, Coordinate coord2, Coordinate coord3)    // -1 means not valid. 0-2 means "this is the non-adjacent cell".
    {
        if (MarkedCoordsContains(coord1) || MarkedCoordsContains(coord2) || MarkedCoordsContains(coord3)) return -1;

        var adjCount = 0;
        var seenCell = 0;

        if (AreCoordinatesAdjacent(coord1, coord2)) { adjCount++; seenCell = 2; }
        if (AreCoordinatesAdjacent(coord1, coord3)) { adjCount++; seenCell = 1; }
        if (AreCoordinatesAdjacent(coord2, coord3)) { adjCount++; seenCell = 0; }

        return adjCount == 1 ? seenCell : -1;
    }

    void Calculate()
    {
        var questions = new[] { (int)SoundType.Question1, (int)SoundType.Question2, (int)SoundType.Question3, (int)SoundType.Question4, (int)SoundType.Question5, (int)SoundType.Question6 };
        var answers = new[] { (int)SoundType.Answer1, (int)SoundType.Answer2, (int)SoundType.Answer3, (int)SoundType.Answer4, (int)SoundType.Answer5, (int)SoundType.Answer6 };

        var allSelectableCoordinates = Enumerable.Range(0, 36).Select(x => Coordinate.GenerateFromPos(x)).ToList();

        AllTriplets = new List<List<DonutSound>>();

        for (int i = 0; i < 3; i++)
        {
            var selected = allSelectableCoordinates.Shuffle().Take(3).ToArray();
            var anomalyIx = 0;

            var logChosenType = false;

            if (Rnd.Range(0, 2) == 0)   // Marked cell?
            {
                logChosenType = true;
                anomalyIx = AreCoordinatesValidMarked(selected[0], selected[1], selected[2]);
                while (true)
                {
                    if (anomalyIx > -1) break;
                    selected = allSelectableCoordinates.Shuffle().Take(3).ToArray();
                    anomalyIx = AreCoordinatesValidMarked(selected[0], selected[1], selected[2]);
                }
            }
            else    // No marked cells?
            {
                anomalyIx = AreCoordinatesValidUnmarked(selected[0], selected[1], selected[2]);
                while (true)
                {
                    if (anomalyIx > -1) break;
                    selected = allSelectableCoordinates.Shuffle().Take(3).ToArray();
                    anomalyIx = AreCoordinatesValidUnmarked(selected[0], selected[1], selected[2]);
                }
            }

            var answer = anomalyIx * 2;
            var sum = selected[anomalyIx].X + selected[anomalyIx].Y;
            if (sum % 2 == 0) answer++;

            ExpectedInput.Add(answer);

            var currentExchange = new List<DonutSound>();
            var logValues = new List<int>();

            for (int j = 0; j < 3; j++)
            {
                var convertedY = selected[j].X + (6 - (FindEdgeworkCalculation(j, false) % 6));
                var convertedN = selected[j].Y + (6 - (FindEdgeworkCalculation(j, true) % 6));

                convertedY %= 6;
                convertedN %= 6;

                logValues.Add(convertedY);
                logValues.Add(convertedN);

                currentExchange.Add(AllSounds[questions[convertedY]]);
                currentExchange.Add(AllSounds[answers[convertedN]]);
            }

            AllTriplets.Add(currentExchange);

            Debug.LogFormat("[Albuquerque #{0}] Stage {1}: The donuts listed are {2}.", _moduleID, i + 1,
                logValues.Select(x => new string[] { "glazed donuts", "jelly donuts", "Bavarian cream-filled donuts", "cinnamon rolls", "apple fritters", "bear claws" }[x]).Join(", "));

            Debug.LogFormat("[Albuquerque #{0}] Therefore, A, B and C are ({1}, {2}), ({3}, {4}) and ({5}, {6}), respectively.", _moduleID, logValues[0], logValues[1], logValues[2], logValues[3], logValues[4], logValues[5]);

            Debug.LogFormat("[Albuquerque #{0}] Adding the edgework calculations and taking each Y and N modulo 6 gives new values of {1}, {2} and {3}.", _moduleID,
                selected[0].ToString(), selected[1].ToString(), selected[2].ToString());

            if (logChosenType)
                Debug.LogFormat("[Albuquerque #{0}] One of the cells lies on a marked cell — this is {1}, which is the anomaly.", _moduleID, "ABC"[anomalyIx]);
            else
                Debug.LogFormat("[Albuquerque #{0}] None of the cells lie on any marked cells, so the non-adjacent cell is the anomaly — this is {1}.", _moduleID, "ABC"[anomalyIx]);

            Debug.LogFormat("[Albuquerque #{0}] The sum of {1}'s Y and N values is {2}, so you should nudge {3}.", _moduleID, "ABC"[anomalyIx], sum % 2 == 0 ? "even" : "odd",
                sum % 2 == 0 ? "Donut Guy" : "Miracle Machine");
        }
    }

    /*private List<DonutSound> GenerateTriplet()
    {
        var questions = new[] { (int)SoundType.Question1, (int)SoundType.Question2, (int)SoundType.Question3, (int)SoundType.Question4, (int)SoundType.Question5, (int)SoundType.Question6 };
        var answers = new[] { (int)SoundType.Answer1, (int)SoundType.Answer2, (int)SoundType.Answer3, (int)SoundType.Answer4, (int)SoundType.Answer5, (int)SoundType.Answer6 };

        return new List<DonutSound>() { AllSounds[questions.PickRandom()], AllSounds[answers.PickRandom()], AllSounds[questions.PickRandom()], AllSounds[answers.PickRandom()], AllSounds[questions.PickRandom()], AllSounds[answers.PickRandom()] };
    }*/

    private void ButtonPress()
    {
        Audio.PlaySoundAtTransform("button press", Button.transform);
        Button.AddInteractionPunch();
        if (ButtonAnimCoroutine != null)
            StopCoroutine(ButtonAnimCoroutine);
        ButtonAnimCoroutine = StartCoroutine(ButtonAnim());
        if (!CannotPress)
            EditSkipRequested = true;
    }

    private void TVPress()
    {
        TVSelectable.AddInteractionPunch();

        if (!CannotPress)
        {
            if (!PlayingSounds)
                StartCoroutine(PlaySounds());
            else
                SkipRequested = true;
        }
    }

    private void StartPlaying()
    {
        ChosenSounds = new List<DonutSound>() { AllSounds[(int)SoundType.Intro] };
        ChosenSounds.AddRange(AllTriplets[CurrentStage]);

        PlayingSounds = true;
        SkipRequested = false;
        EditSkipRequested = false;

        SetLEDs(true, false);
        TV.color = Color.white;
        PlayButton.color = Color.clear;
        PlayButton.sprite = ReplaySprite;
    }

    private void StopPlaying()
    {
        PlayingSounds = false;
        SetLEDs(false, true);
        TV.color = new Color(0.65f, 0.65f, 0.65f);
        PlayButton.color = Color.white;
        if (Sound != null)
            Sound.StopSound();      // Just in case, y'know.
    }

    private void SetLEDs(bool isGreenOn, bool isRedOn)
    {
        if (isGreenOn)
        {
            GreenLED.material = LEDMats[0];
            GreenLED.material.color = Color.green;
            GreenLEDGlow.color = new Color(0, 1, 0, 0.5f);
        }
        else
        {
            GreenLED.material = LEDMats[1];
            GreenLED.material.color = Color.green * (3 / 8f);
            GreenLEDGlow.color = Color.clear;
        }
        if (isRedOn)
        {
            RedLED.material = LEDMats[0];
            RedLED.material.color = Color.red;
            RedLEDGlow.color = new Color(1, 0, 0, 0.5f);
        }
        else
        {
            RedLED.material = LEDMats[1];
            RedLED.material.color = Color.red * (3 / 8f);
            RedLEDGlow.color = Color.clear;
        }
    }

    private IEnumerator PlaySounds()
    {
        StartPlaying();

        for (int i = 0; i < ChosenSounds.Count(); i++)
        {
            CurrentPhrase = i;

            if (Sound != null)
                Sound.StopSound();
            Sound = Audio.HandlePlaySoundAtTransformWithRef(ChosenSounds[i].GetName(), transform, false);

            TV.sprite = FindSprite(ChosenSounds[i].GetName());

            float timer = 0;
            float duration = ChosenSounds[i].GetLength() - 0.01f;     // To lessen the gaps.
            while (timer < duration)
            {
                if (((TPAutoSkip && timer > 0.1f) || SkipRequested) && i < ChosenSounds.Count() - 1)
                {
                    yield return PlayStatic();
                    break;
                }
                else if (EditSkipRequested)
                {
                    yield return PlayStatic();
                    if (Sound != null)
                        Sound.StopSound();
                    if ((CurrentStage == 0 && ExpectedInput[CurrentStage] == i - 1) || (CurrentStage > 0 && ExpectedInput[CurrentStage] == i))
                    {
                        if (CurrentStage == 2)
                        {
                            Debug.LogFormat("[Albuquerque #{0}] All three nudges were correct — module solved!", _moduleID);
                            CannotPress = true;
                            Sound = Audio.HandlePlaySoundAtTransformWithRef(AllSounds[(int)SoundType.Outro].GetName(), transform, false);
                            TV.sprite = FindSprite(AllSounds[(int)SoundType.Outro].GetName());
                            timer = 0;
                            duration = AllSounds[(int)SoundType.Outro].GetLength() - 0.01f;     // To lessen the gaps.
                            while (timer < duration)
                            {
                                yield return null;
                                timer += Time.deltaTime;
                            }
                            yield return new WaitForSeconds(0.25f);
                            StartCoroutine(HandlePass());
                            if (Sound != null)
                                Sound.StopSound();
                            Sound = Audio.HandlePlaySoundAtTransformWithRef(AllSounds[(int)SoundType.Solve].GetName(), transform, false);
                            TV.sprite = FindSprite(AllSounds[(int)SoundType.Solve].GetName());
                            timer = 0;
                            duration = AllSounds[(int)SoundType.Solve].GetLength() - 0.01f - 1.75f;
                            while (timer < duration)
                            {
                                yield return null;
                                timer += Time.deltaTime;
                            }
                            goto end;
                        }
                        else
                        {
                            CurrentStage++;
                            ChosenSounds = AllTriplets[CurrentStage];
                            CurrentPhrase = i = -1;     // Because for loops. :)
                            break;
                        }
                    }
                    else
                    {
                        TPStruck = true;
                        Debug.LogFormat("[Albuquerque #{0}] You nudged {1} during his {2} phrase, on Stage {3}, which was incorrect. Strike!", _moduleID,
                            (CurrentStage == 0 && i % 2 == 0) || (CurrentStage > 0 && i % 2 == 1) ? "Donut Guy" : "Miracle Machine",
                            new[] { "first", "second", "third" }[CurrentStage], CurrentStage + 1);
                        Sound = Audio.HandlePlaySoundAtTransformWithRef(AllSounds[(int)SoundType.Strike].GetName(), transform, false);
                        TV.sprite = FindSprite(AllSounds[(int)SoundType.Strike].GetName());
                        StartCoroutine(HandleStrike());
                        timer = 0;
                        duration = AllSounds[(int)SoundType.Strike].GetLength() - 0.01f;     // To lessen the gaps.
                        while (timer < duration)
                        {
                            yield return null;
                            timer += Time.deltaTime;
                        }
                        TPStruck = false;
                        goto end;
                    }
                }
                yield return null;
                timer += Time.deltaTime;
            }
        }

        end:
        CurrentStage = 0;
        yield return new WaitForSeconds(0.25f);
        if (!CannotPress)
        {
            if (Sound != null)
                Sound.StopSound();
            StopPlaying();
        }
        else
            StartCoroutine(StopPlayingSolved());
    }

    private IEnumerator HandleStrike()
    {
        yield return "strike";
        Module.HandleStrike();
    }

    private IEnumerator HandlePass()
    {
        yield return "solve";
        Module.HandlePass();
    }

    private IEnumerator StopPlayingSolved(float duration = 2f)
    {
        PlayingSounds = false;
        SetLEDs(false, true);

        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            TV.color = Color.Lerp(Color.white, Color.black, timer / duration);
        }
        TV.color = Color.black;
    }

    private IEnumerator PlayStatic()
    {
        if (Sound != null)
            Sound.StopSound();
        Sound = Audio.HandlePlaySoundAtTransformWithRef("static", transform, false);
        for (int j = 0; j < 3; j++)
        {
            TV.sprite = FindSprite("noise " + (j + 1));
            yield return new WaitForSeconds(1 / 15f);
        }
        SkipRequested = false;
        EditSkipRequested = false;
    }

    private IEnumerator ButtonAnim(float inDuration = 0.1f, float outDuration = 0.25f, float depression = 0.25f)
    {
        Button.transform.localScale = new Vector3(Button.transform.localScale.x, Button.transform.localScale.y, 1);
        float timer = 0;
        while (timer < inDuration)
        {
            Button.transform.localScale = new Vector3(Button.transform.localScale.x, Button.transform.localScale.y, Easing.OutSine(timer, 1, depression, inDuration));
            yield return null;
            timer += Time.deltaTime;
        }
        Button.transform.localScale = new Vector3(Button.transform.localScale.x, Button.transform.localScale.y, depression);
        timer = 0;
        while (timer < outDuration)
        {
            Button.transform.localScale = new Vector3(Button.transform.localScale.x, Button.transform.localScale.y, Easing.BackOut(timer, depression, 1, outDuration, 2));
            yield return null;
            timer += Time.deltaTime;
        }
        Button.transform.localScale = new Vector3(Button.transform.localScale.x, Button.transform.localScale.y, 1);
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} play' to press the TV. Use '!{0} m2' to press the TV, then press the button when Miracle Machine is saying his second phrase ('d' for Donut Guy). Button presses may be chained with spaces.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();

        if (PlayingSounds)
        {
            yield return "sendtochaterror Please wait for the current loop to finish playing.";
            yield break;
        }

        if (command == "play")
        {
            yield return null;
            TPAutoSkip = false;
            TVSelectable.OnInteract();
            yield break;
        }

        var commandArray = command.Split(' ');
        if (commandArray.Length < 1 || commandArray.Length > 3)
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }

        for (int i = 0; i < commandArray.Length; i++)
            if (commandArray[i].Length != 2 || !"md".Contains(commandArray[i][0]) || !"123".Contains(commandArray[i][1]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }

        yield return null;
        TPAutoSkip = true;
        TVSelectable.OnInteract();

        for (int i = 0; i < commandArray.Length; i++)
        {
            var phraseToWaitFor = ((int.Parse(commandArray[i][1].ToString()) - 1) * 2) + (commandArray[i][0] == 'd' ? 1 : 0);
            if (CurrentStage == 0) phraseToWaitFor++;

            while (!(CurrentPhrase == phraseToWaitFor || !PlayingSounds))
                yield return "trycancel Command cancelled.";

            if (!PlayingSounds)
                yield break;
            Button.OnInteract();

            var oldStage = CurrentStage;
            while (!(oldStage != CurrentStage || TPStruck || CannotPress))
                yield return "trycancel Command cancelled.";
            if (CannotPress)
            {
                TPAutoSkip = false;
                yield break;
            }
            TPStruck = false;
        }
        TPAutoSkip = false;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        TPAutoSkip = true;

        while (PlayingSounds)
            yield return true;

        yield return null;
        TVSelectable.OnInteract();

        for (int i = 0; i < ExpectedInput.Count(); i++)
        {
            var phraseToWaitFor = ExpectedInput[i];
            if (CurrentStage == 0) phraseToWaitFor++;

            while (!(CurrentPhrase == phraseToWaitFor || !PlayingSounds))
                yield return null;

            if (!PlayingSounds)
                yield break;
            Button.OnInteract();

            var oldStage = CurrentStage;
            while (!(oldStage != CurrentStage || CannotPress))
                yield return null;
            if (CannotPress)
            {
                TPAutoSkip = false;
                yield break;
            }
        }
    }
}
