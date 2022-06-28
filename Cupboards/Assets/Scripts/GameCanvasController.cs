using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class GameCanvasController : MonoBehaviour
{
    [SerializeField] private GameboardController gameboardController;
    [SerializeField] private TextMeshProUGUI errorMessage;
    private bool isRightData;
    private int readLineCounter;

    public string DataStatusMessage
    {
        get
        {
            if (isRightData)
                return string.Empty;
            else
                return $"Некорректные данные в {readLineCounter} строке";
        }
    }

    private void ResetErrorAndCounter()
    {
        isRightData = true;
        readLineCounter = 0;
    }

    public void GetInputData()
    {
        if (!gameboardController.ArePreparingToMoveOrMoving)
        {
            ResetErrorAndCounter();

            string[] path = StandaloneFileBrowser.OpenFilePanel("Open file", "", "", false);

            if (path != null && path.Length > 0)
            {
                int numberOfChips;
                int numberOfPlaces;
                Vector2[] placeCoordinates;
                int[] atStartChipOnPlaceIncrementedIndexes;
                int[] atWinChipOnPlaceIncrementedIndexes;
                int numberOfWays;
                int[][] waysByIncrementedIndexes;

                using (StreamReader streamReader = new StreamReader(path[0]))
                {
                    numberOfChips = GetIntParametersFromLine(1)[0];
                    numberOfPlaces = GetIntParametersFromLine(1)[0];
                    placeCoordinates = GetPlaceCoordinates(numberOfPlaces);
                    atStartChipOnPlaceIncrementedIndexes = GetIntParametersFromLine(numberOfChips);
                    atWinChipOnPlaceIncrementedIndexes = GetIntParametersFromLine(numberOfChips);
                    numberOfWays = GetIntParametersFromLine(1)[0];
                    waysByIncrementedIndexes = GetWaysByIncrementedIndexes(numberOfWays);

                    int[] GetIntParametersFromLine(int numberOfParameters)
                    {
                        int[] parameters = new int[numberOfParameters];

                        if (isRightData)
                        {
                            string[] rawParameters = ReadLine().Split(',');
                            isRightData = rawParameters.Length == numberOfParameters;

                            for (int i = 0; i < numberOfParameters && isRightData; i++)
                                isRightData = int.TryParse(rawParameters[i], out parameters[i]);
                        }

                        return parameters;
                    }

                    Vector2[] GetPlaceCoordinates(int numberOfPlaces)
                    {
                        Vector2[] placeCoordinates = new Vector2[numberOfPlaces];

                        if (isRightData)
                        {
                            for (int i = 0; i < numberOfPlaces && isRightData; i++)
                                placeCoordinates[i] = GetOneVector2ParameterFromLine();
                        }

                        return placeCoordinates;

                        Vector2 GetOneVector2ParameterFromLine()
                        {
                            if (isRightData)
                            {
                                float[] parameters = GetFloatParametersFromLine(2);
                                if (isRightData)
                                    return new Vector2(parameters[0], parameters[1]);
                            }

                            return new Vector2(default, default);
                        }

                        float[] GetFloatParametersFromLine(int numberOfParameters)
                        {
                            float[] parameters = new float[numberOfParameters];

                            if (isRightData)
                            {
                                string[] rawParameters = ReadLine().Split(',');
                                isRightData = rawParameters.Length == numberOfParameters;

                                for (int i = 0; i < numberOfParameters && isRightData; i++)
                                    isRightData = float.TryParse(rawParameters[i], out parameters[i]);
                            }

                            return parameters;
                        }
                    }

                    int[][] GetWaysByIncrementedIndexes(int numberOfWays)
                    {
                        int[][] ways = new int[numberOfWays][];

                        if (isRightData)
                        {
                            for (int i = 0; i < numberOfWays && isRightData; i++)
                                ways[i] = GetIntParametersFromLine(2);
                        }

                        return ways;
                    }

                    string ReadLine()
                    {
                        string line = streamReader.ReadLine();
                        readLineCounter++;

                        if (line != null)
                            return line;
                        else
                            return string.Empty;
                    }
                }

                errorMessage.text = DataStatusMessage;

                if (isRightData)
                    gameboardController.Initialize(placeCoordinates, waysByIncrementedIndexes, atStartChipOnPlaceIncrementedIndexes, atWinChipOnPlaceIncrementedIndexes);
            }
        }
    }

    public void Quit() => Application.Quit();
}
