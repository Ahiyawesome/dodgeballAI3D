
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using System.IO;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using System.Globalization;
using UnityEngine.UIElements;
using JetBrains.Annotations;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class DeepLearning : MonoBehaviour
{
    private static int CONTROL_JOINTS = 17 * 3; // 17 Character Joints + x, y, and z values for each
    static int TIME_STEP = 70;
    static int ANIMS = 4;
    //                              Node From          Node To        
    private double[,] wf = new double[CONTROL_JOINTS + 1, CONTROL_JOINTS * 4 + 7];
    private double[] uf = new double[CONTROL_JOINTS + 1];
    private double[] bf = new double[CONTROL_JOINTS + 1];
    private double[,] wi = new double[CONTROL_JOINTS + 1, CONTROL_JOINTS * 4 + 7];
    private double[] ui = new double[CONTROL_JOINTS + 1];
    private double[] bi = new double[CONTROL_JOINTS + 1];
    private double[,] wc = new double[CONTROL_JOINTS + 1, CONTROL_JOINTS * 4 + 7];
    private double[] uc = new double[CONTROL_JOINTS + 1];
    private double[] bc = new double[CONTROL_JOINTS + 1];
    private double[,] wo = new double[CONTROL_JOINTS + 1, CONTROL_JOINTS * 4 + 7];
    private double[] uo = new double[CONTROL_JOINTS + 1];
    private double[] bo = new double[CONTROL_JOINTS + 1];

    private Transform[] x_trsm = new Transform[CONTROL_JOINTS / 3];
    private Transform ball;

    private Rigidbody[] rbs = new Rigidbody[CONTROL_JOINTS / 3];
    private GameObject player;
    private GameObject playerCopy;
    public GameObject prefab_ball;
    private double[] rotPrev = new double[CONTROL_JOINTS];
    private double[] rotPrevx = new double[CONTROL_JOINTS];


    // x
    private double[,] inputs = new double[ANIMS, CONTROL_JOINTS * 4 + 7];

    // ft
    private double[,] forgetGate = new double[ANIMS + 1, CONTROL_JOINTS + 1];

    // it
    private double[,] inputGate = new double[ANIMS, CONTROL_JOINTS + 1];

    // Ct (curly)
    private double[,] updateCandidate = new double[ANIMS, CONTROL_JOINTS + 1];

    // ot
    private double[,] outputCell = new double[ANIMS, CONTROL_JOINTS + 1];

    // Ct
    private double[,] finalCandidate = new double[ANIMS + 1, CONTROL_JOINTS + 1];

    // ht (or yt)
    private double[,] finalOutput = new double[ANIMS, CONTROL_JOINTS + 1];

    // ht-1 
    private double[,] prevOutput = new double[ANIMS, CONTROL_JOINTS + 1];


    // DEEP LEARNING

    // inputs
    private double[] inputOutputs = new double[ANIMS * CONTROL_JOINTS * 4 + 7];

    private double[] hiddenOne = new double[500];

    private double[] hiddenTwo = new double[300];

    private double[] moveOutput = new double[CONTROL_JOINTS + 1];

    private double[,] weightItO = new double[500, (ANIMS * CONTROL_JOINTS * 4 + 7)];
    private double[,] weightOtT = new double[300, 500];
    private double[,] weightTtOut = new double[CONTROL_JOINTS + 1, 300];

    private double[] dCost = new double[300];


    int test = 50000;

    int frames = 0;
    private double gamma = 0.01;
    bool inHand = true;
    bool usingPast = false;
    bool alreadyDone = false;
    double count = 0.0f;
    void Start()
    {
        player = this.transform.GetChild(0).gameObject;
        firstSets();
        //setPositions();
        playerCopy = GameObject.Instantiate(player, this.transform);
        playerCopy.SetActive(false);

        if (usingPast)
        {
            string[] lines = File.ReadAllLines(Application.dataPath + "/network_d.txt");
            string[] dlines = new string[ANIMS];

            for (int i = 0; i < 3; i++)
            {
                dlines[i] = lines[lines.Length - (3 - i)];
            }
     
            foreach (string lin in dlines)
            {
                string[] d = lin.Split(' ');

                /*int ind = 0;
                for (int k = 0; k < CONTROL_JOINTS * 4 + 7; k++)
                {
                    wf[j, k] = double.Parse(d[ind], CultureInfo.InvariantCulture.NumberFormat);
                    wi[j, k] = double.Parse(d[ind + 1], CultureInfo.InvariantCulture.NumberFormat);
                    wc[j, k] = double.Parse(d[ind + 2], CultureInfo.InvariantCulture.NumberFormat);
                    wo[j, k] = double.Parse(d[ind + 3], CultureInfo.InvariantCulture.NumberFormat);
                    ind += 4;
                }
                uf[j] = double.Parse(d[ind], CultureInfo.InvariantCulture.NumberFormat);
                ui[j] = double.Parse(d[ind + 1], CultureInfo.InvariantCulture.NumberFormat);
                uc[j] = double.Parse(d[ind + 2], CultureInfo.InvariantCulture.NumberFormat);
                uo[j] = double.Parse(d[ind + 3], CultureInfo.InvariantCulture.NumberFormat);
                bf[j] = double.Parse(d[ind + 4], CultureInfo.InvariantCulture.NumberFormat);
                bi[j] = double.Parse(d[ind + 5], CultureInfo.InvariantCulture.NumberFormat);
                bc[j] = double.Parse(d[ind + 6], CultureInfo.InvariantCulture.NumberFormat);
                bo[j] = double.Parse(d[ind + 7], CultureInfo.InvariantCulture.NumberFormat);

                j++;*/
                int ind = 0;
                for (int i = 0; i < (500); i++)
                {
                    for (int j = 0; j < (ANIMS * CONTROL_JOINTS * 4 + 7); j++)
                    {
                        weightItO[i, j] = double.Parse(d[ind], CultureInfo.InvariantCulture.NumberFormat);
                        ind++;
                    }
                }
                for (int i = 0; i < (300); i++)
                {
                    for (int j = 0; j < 500; j++)
                    {
                        weightOtT[i, j] = double.Parse(d[ind], CultureInfo.InvariantCulture.NumberFormat);
                        ind++;
                    }
                }
                for (int i = 0; i < (CONTROL_JOINTS + 1); i++)
                {
                    for (int j = 0; j < 300; j++)
                    {
                        weightTtOut[i, j] = double.Parse(d[ind], CultureInfo.InvariantCulture.NumberFormat);
                        ind++;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < (500); i++)
            {
                for (int j = 0; j < (ANIMS * CONTROL_JOINTS * 4 + 7); j++)
                {
                    weightItO[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                }
            }
            for (int i = 0; i < (300); i++)
            {
                for (int j = 0; j < 500; j++)
                {
                    weightOtT[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                }
            }
            for (int i = 0; i < (CONTROL_JOINTS + 1); i++)
            {
                for (int j = 0; j < 300; j++)
                {
                    weightTtOut[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                }
            }

        }

    }
    void FixedUpdate()
    {

        int join = 0;
        for (int i = 0; i < CONTROL_JOINTS; i++)
        {
            inputs[frames, i] = x_trsm[join].transform.rotation.x;
            inputs[frames, i + 1] = x_trsm[join].transform.rotation.y;
            inputs[frames, i + 2] = x_trsm[join].transform.rotation.z;
            i += 2;
            join++;

        }
        join = 0;
        for (int i = CONTROL_JOINTS; i < CONTROL_JOINTS * 2; i++)
        {
            inputs[frames, i] = x_trsm[join].transform.position.x;
            inputs[frames, i + 1] = x_trsm[join].transform.position.y;
            inputs[frames, i + 2] = x_trsm[join].transform.position.z;
            i += 2;
            join++;
        }
        join = 0;
        for (int i = CONTROL_JOINTS * 2; i < CONTROL_JOINTS * 3; i++)
        {
            inputs[frames, i] = rbs[join].velocity.x;
            inputs[frames, i + 1] = rbs[join].velocity.x;
            inputs[frames, i + 2] = rbs[join].velocity.x;
            i += 2;
            join++;
        }
        join = 0;
        for (int i = CONTROL_JOINTS * 3; i < CONTROL_JOINTS * 4; i++)
        {
            inputs[frames, i] = rbs[join].angularVelocity.x;
            inputs[frames, i + 1] = rbs[join].angularVelocity.x;
            inputs[frames, i + 2] = rbs[join].angularVelocity.x;
            i += 2;
            join++;
        }


        if (frames == 0)
        {
            Debug.Log(test);
        }
        if (frames < TIME_STEP)
        {

            inputs[frames, CONTROL_JOINTS * 4] = ball.position.x;
            inputs[frames, CONTROL_JOINTS * 4 + 1] = ball.position.y;
            inputs[frames, CONTROL_JOINTS * 4 + 2] = ball.position.z;
            inputs[frames, CONTROL_JOINTS * 4 + 3] = ball.GetComponent<Rigidbody>().velocity.x;
            inputs[frames, CONTROL_JOINTS * 4 + 4] = ball.GetComponent<Rigidbody>().velocity.y;
            inputs[frames, CONTROL_JOINTS * 4 + 5] = ball.GetComponent<Rigidbody>().velocity.z;
            inputs[frames, CONTROL_JOINTS * 4 + 6] = (inHand ? 1 : 0);


            LSTM_Forward_Prop();
            count += calculate_error();

        }
        else if (frames > TIME_STEP + 3)
        {
            frames = -1;
            resetDelOut();
            test--;
            if (test % 800 == 0)
            {
                writeStuff(false);
                
            }
            count = 0;
            inHand = true;
            Physics.autoSimulation = true;
            alreadyDone = false;
        }
        else if (frames >= TIME_STEP && test > 0 && !alreadyDone)
        {
            resetPlayer();
            Physics.autoSimulation = false;
            LSTM_Back_Prop();
            alreadyDone = true;
        }
        frames++;

    }

    void LSTM_Forward_Prop()
    {
        double summation;
        // Calculate forget layer
        int index = 0;
        for (int a = 0; a < ANIMS; a++)
        {
            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                summation = 0;
                for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
                {
                    summation += (inputs[frames, j] * wf[i, j]);

                }
                if (frames != 0)
                    forgetGate[a, i] = sigma(summation + (prevOutput[a, i] * uf[i]) + bf[i]);
                else
                    forgetGate[a, i] = sigma(summation + bf[i]);

                //if (a == 5) Debug.Log("forget: " + forgetGate[a, i]);


            }

            // Calculate update layer
            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                summation = 0;
                for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
                {
                    summation += (inputs[a, j] * wi[i, j]);

                }
                if (frames != 0)
                    inputGate[a, i] = sigma(summation + (prevOutput[a, i] * ui[i]) + bi[i]);
                else
                    inputGate[a, i] = sigma(summation + bi[i]);
                // if (a == 5) Debug.Log("input: " + inputGate[a, i]);

            }

            // Calculate candidate layer
            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                summation = 0;
                for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
                {
                    summation += (inputs[a, j] * wc[i, j]);

                }
                if (frames != 0)
                    updateCandidate[a, i] = tanH(summation + (prevOutput[a , i] * uc[i]) + bc[i]);
                else
                    updateCandidate[a, i] = tanH(summation + bc[i]);
                //if (a == 5) Debug.Log("updateCandidate: " + updateCandidate[a, i]);
            }

            // Calculate output layer
            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                summation = 0;
                for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
                {
                    summation += (inputs[a, j] * wo[i, j]);

                }
                if (frames != 0)
                    outputCell[a, i] = sigma(summation + (prevOutput[a , i] * uo[i]) + bo[i]);
                else
                    outputCell[a, i] = sigma(summation + bo[i]);

                //if (a == 5) Debug.Log("outputGate: " + outputCell[a, i]);
            }

            // Calculate ct-1
            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                finalCandidate[a, i] = (inputGate[a, i] * updateCandidate[a, i]);
                if (frames != 0) finalCandidate[a, i] += (finalCandidate[a - 1, i] * forgetGate[a, i]);

                //if (a == 5) Debug.Log("finalCandidate: " + finalCandidate[a, i]);
            }

            // Calculate final output
            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                finalOutput[a, i] = outputCell[a, i] * tanH(finalCandidate[a, i]);
                inputOutputs[index] = finalOutput[a, i];
                prevOutput[a, i] = finalOutput[a, i];
                //if (a == 5) Debug.Log("output: " + finalOutput[a, i]);

            }
        }

        for (int i = 0; i < (500); i++)
        {
            summation = 0;
            for (int j = 0; j < (ANIMS * CONTROL_JOINTS * 4 + 7); j++)
            {
                summation += inputOutputs[j] * weightItO[i, j];
            }
            hiddenOne[i] = sigma(summation);
        }
        for (int i = 0; i < (300); i++)
        {
            summation = 0;
            for (int j = 0; j < (500); j++)
            {
                summation += hiddenOne[j] * weightOtT[i, j];
            }
            hiddenTwo[i] = sigma(summation);
        }
        for (int i = 0; i < (CONTROL_JOINTS + 1); i++)
        {
            summation = 0;
            for (int j = 0; j < (300); j++)
            {
                summation += hiddenTwo[j] * weightTtOut[i, j];
            }
            moveOutput[i] = sigma(summation);
        }

        // move the player
        for (int i = 0; i < CONTROL_JOINTS / 3; i++)
        {
            x_trsm[i].transform.Rotate((float)moveOutput[i * 3] * 10, (float)moveOutput[i * 3 + 1] * 10, (float)moveOutput[i * 3 + 2] * 10, Space.World);
        }

        if (moveOutput[CONTROL_JOINTS] > 0 && inHand)
        {
            ball.GetComponent<Rigidbody>().isKinematic = false;
            ball.transform.SetParent(null);
            inHand = false;
        }


    }
            
double calculate_error()
    {
        double MSE = 0.0f;

        for (int i = 0; i < CONTROL_JOINTS; i++)
        {
            //MSE += Math.Pow((x[i] - targs[a, i]), 2);
        }
        MSE *= (1.0f / CONTROL_JOINTS);

        //Debug.Log("MSE: " + MSE.ToString());

        return MSE;
    }

    void LSTM_Back_Prop()
    {
       
    }

    void resetPlayer()
    {
        Destroy(ball.gameObject);
        Destroy(player);
        player = GameObject.Instantiate(playerCopy, this.transform);
        player.SetActive(true);
        setRbsAndTrsfs();
    }

    double sigma(double x)
    {
        Debug.Log("SIGMA: " + (1.0f / (1.0f + Math.Exp(-x))));

        // So that it doesn't divide by 0;
        if (Double.IsNaN(1.0f / (1.0f + Math.Exp(-x)))) return (0.0f);
        else return 1.0f / (1.0f + Math.Exp(-x));
    }

    double tanH(double x)
    {
        Debug.Log("TANH: " + ((Math.Exp(x) - Math.Exp(-x)) / (Math.Exp(x) + Math.Exp(-x))));

        if (Double.IsNaN((Math.Exp(x) - Math.Exp(-x)) / (Math.Exp(x) + Math.Exp(-x)))) return (0.0f);

        else return ((Math.Exp(x) - Math.Exp(-x)) / (Math.Exp(x) + Math.Exp(-x)));
    }
    void firstSets()
    {
        x_trsm[0] = player.transform.Find("Slash_Young_Male.001/hips_JNT");
        x_trsm[1] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT");
        x_trsm[2] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT/l_leg_JNT");
        x_trsm[3] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT/l_leg_JNT/l_foot_JNT/l_toebase_JNT");
        x_trsm[4] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT");
        x_trsm[5] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT/r_leg_JNT");
        x_trsm[6] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT/r_leg_JNT/r_foot_JNT/r_toebase_JNT");
        x_trsm[7] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT");
        x_trsm[8] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT");
        x_trsm[9] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT");
        x_trsm[10] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT");
        x_trsm[11] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT/l_handMiddle1_JNT/l_handMiddle2_JNT");
        x_trsm[12] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/neck_JNT");
        x_trsm[13] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT");
        x_trsm[14] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT");
        x_trsm[15] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT");
        x_trsm[16] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT");

        rbs[0] = player.transform.Find("Slash_Young_Male.001/hips_JNT").GetComponent<Rigidbody>();
        rbs[1] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT").GetComponent<Rigidbody>();
        rbs[2] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT/l_leg_JNT").GetComponent<Rigidbody>();
        rbs[3] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT/l_leg_JNT/l_foot_JNT/l_toebase_JNT").GetComponent<Rigidbody>();
        rbs[4] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT").GetComponent<Rigidbody>();
        rbs[5] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT/r_leg_JNT").GetComponent<Rigidbody>();
        rbs[6] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT/r_leg_JNT/r_foot_JNT/r_toebase_JNT").GetComponent<Rigidbody>();
        rbs[7] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT").GetComponent<Rigidbody>();
        rbs[8] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT").GetComponent<Rigidbody>();
        rbs[9] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT").GetComponent<Rigidbody>();
        rbs[10] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT").GetComponent<Rigidbody>();
        rbs[11] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT/l_handMiddle1_JNT/l_handMiddle2_JNT").GetComponent<Rigidbody>();
        rbs[12] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/neck_JNT").GetComponent<Rigidbody>();
        rbs[13] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT").GetComponent<Rigidbody>();
        rbs[14] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT").GetComponent<Rigidbody>();
        rbs[15] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT").GetComponent<Rigidbody>();
        rbs[16] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT").GetComponent<Rigidbody>();


        ball = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT/Ball");

    }
    void setRbsAndTrsfs()
    {
        x_trsm[0] = player.transform.Find("Slash_Young_Male.001/hips_JNT");
        x_trsm[1] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT");
        x_trsm[2] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT/l_leg_JNT");
        x_trsm[3] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT/l_leg_JNT/l_foot_JNT/l_toebase_JNT");
        x_trsm[4] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT");
        x_trsm[5] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT/r_leg_JNT");
        x_trsm[6] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT/r_leg_JNT/r_foot_JNT/r_toebase_JNT");
        x_trsm[7] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT");
        x_trsm[8] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT");
        x_trsm[9] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT");
        x_trsm[10] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT");
        x_trsm[11] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT/l_handMiddle1_JNT/l_handMiddle2_JNT");
        x_trsm[12] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/neck_JNT");
        x_trsm[13] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT");
        x_trsm[14] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT");
        x_trsm[15] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT");
        x_trsm[16] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT");

        rbs[0] = player.transform.Find("Slash_Young_Male.001/hips_JNT").GetComponent<Rigidbody>();
        rbs[1] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT").GetComponent<Rigidbody>();
        rbs[2] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT/l_leg_JNT").GetComponent<Rigidbody>();
        rbs[3] = player.transform.Find("Slash_Young_Male.001/hips_JNT/l_upleg_JNT/l_leg_JNT/l_foot_JNT/l_toebase_JNT").GetComponent<Rigidbody>();
        rbs[4] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT").GetComponent<Rigidbody>();
        rbs[5] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT/r_leg_JNT").GetComponent<Rigidbody>();
        rbs[6] = player.transform.Find("Slash_Young_Male.001/hips_JNT/r_upleg_JNT/r_leg_JNT/r_foot_JNT/r_toebase_JNT").GetComponent<Rigidbody>();
        rbs[7] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT").GetComponent<Rigidbody>();
        rbs[8] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT").GetComponent<Rigidbody>();
        rbs[9] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT").GetComponent<Rigidbody>();
        rbs[10] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT").GetComponent<Rigidbody>();
        rbs[11] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT/l_handMiddle1_JNT/l_handMiddle2_JNT").GetComponent<Rigidbody>();
        rbs[12] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/neck_JNT").GetComponent<Rigidbody>();
        rbs[13] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT").GetComponent<Rigidbody>();
        rbs[14] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT").GetComponent<Rigidbody>();
        rbs[15] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT").GetComponent<Rigidbody>();
        rbs[16] = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT").GetComponent<Rigidbody>();

        ball = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT/Ball");



    }

    void writeStuff(bool best)
    {
        void writeStuff(bool best)
        {
            /*if (best)
            {
                for (int i = 0; i < CONTROL_JOINTS; i++)
                {
                    File.AppendAllText(Application.dataPath + "/best_network.txt", "" + wf[i] + " " + uf[i] + " " + bf[i] + " " + wi[i] + " " + ui[i] + " " + bi[i] + " " + wc[i] + " " + uc[i] + " " + bc[i] + " " + wo[i] + " " + uo[i] + " " + bo[i] + "\n");
                }
                File.AppendAllText(Application.dataPath + "/best_network.txt", "\n");
            }
            else*/
            string s = "";
            for (int i = 0; i < (500); i++)
            {
                for (int j = 0; j < (ANIMS * CONTROL_JOINTS * 4 + 7); j++)
                {
                    s += weightItO[i, j];
                }
            }
            s += "\n";
            for (int i = 0; i < (300); i++)
            {
                for (int j = 0; j < 500; j++)
                {
                    s += weightOtT[i, j];
                }
            }
            s += "\n";
            for (int i = 0; i < (CONTROL_JOINTS + 1); i++)
            {
                for (int j = 0; j < 300; j++)
                {
                    s += weightTtOut[i, j];
                }
            }
            s += "\n";
            File.AppendAllText(Application.dataPath + "/network_d.txt", s + "\n");


        }



    }
    void resetDelOut()
    {

        for (int i = 0; i < CONTROL_JOINTS; i++)
        {
            rotPrevx[i] = 0.0f;
            rotPrev[i] = 0.0f;
        }
        // reset del weights

    }
}



