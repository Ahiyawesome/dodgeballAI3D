
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

public class AI_2 : MonoBehaviour
{
    private static int CONTROL_JOINTS = 17 * 3; // 17 Character Joints + x, y, and z values for each
    static int FRAMES = 51;
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
    private Transform[] target_trsm = new Transform[CONTROL_JOINTS / 3];
    private Animator anim;
    private Transform ball;

    private Transform targBall;
    private Rigidbody[] rbs = new Rigidbody[CONTROL_JOINTS / 3];
    private Rigidbody[] targrbs = new Rigidbody[CONTROL_JOINTS / 3];
    public GameObject perfect;
    private GameObject player;
    private GameObject playerCopy;
    public GameObject prefab_ball;
    private double[] rotPrev = new double[CONTROL_JOINTS + 1];
    private double[] rotPrevx = new double[CONTROL_JOINTS + 1];

    // target_vals
    private double[,] targs = new double[FRAMES, CONTROL_JOINTS];

    // x
    private double[,] x = new double[FRAMES, CONTROL_JOINTS];

    // x
    private double[,] inputs = new double[FRAMES, CONTROL_JOINTS * 4 + 7];

    // ft
    private double[,] forgetGate = new double[FRAMES + 1, CONTROL_JOINTS + 1];

    // it
    private double[,] inputGate = new double[FRAMES, CONTROL_JOINTS + 1];

    // Ct (curly)
    private double[,] updateCandidate = new double[FRAMES, CONTROL_JOINTS + 1];

    // ot
    private double[,] outputCell = new double[FRAMES, CONTROL_JOINTS + 1];

    // Ct
    private double[,] finalCandidate = new double[FRAMES + 1, CONTROL_JOINTS + 1];

    // ht (or yt)
    private double[,] finalOutput = new double[FRAMES, CONTROL_JOINTS + 1];

    // dE
    private double[,] errorDerv = new double[FRAMES, CONTROL_JOINTS + 1];

    // dOut
    private double[,] dOut = new double[FRAMES, CONTROL_JOINTS + 1];

    // dCt
    private double[,] dFinalCandidate = new double[FRAMES + 1, CONTROL_JOINTS + 1];

    // delta out
    private double[,] del_out = new double[FRAMES + 1, CONTROL_JOINTS + 1];

    // dCt
    private double[,] dUpdateCandidate = new double[FRAMES, CONTROL_JOINTS + 1];

    // dit
    private double[,] dInput = new double[FRAMES, CONTROL_JOINTS + 1];

    // df
    private double[,] dForget = new double[FRAMES, CONTROL_JOINTS + 1];

    // do1
    private double[,] dOutputCell = new double[FRAMES, CONTROL_JOINTS + 1];

    // weight gradient
    private double[,,] dW = new double[4, CONTROL_JOINTS + 1, CONTROL_JOINTS * 4 + 7];

    // prev weight gradient
    private double[,] dU = new double[4, CONTROL_JOINTS + 1];

    // bias gradient
    private double[,] dB = new double[4, CONTROL_JOINTS + 1];

    int test = 50000;

    int frames = 0;
    private double gamma = 0.00001;
    bool inHand = true;
    bool targInHand = true;
    bool usingPast = true;
    bool alreadyDone = false;
    double count = 0.0f;
    bool written = false;
    void Start()
    {
        player = this.transform.GetChild(0).gameObject;
        firstSets();
        //setPositions();
        playerCopy = GameObject.Instantiate(player, this.transform);
        playerCopy.SetActive(false);
        anim = perfect.GetComponent<Animator>();

        for (int t = 0; t < FRAMES; t++)
        {
            for (int i = 0; i < CONTROL_JOINTS / 3; i++)
            {
                x[t, i * 3] = x_trsm[i].transform.rotation.x;
                x[t, i * 3 + 1] = x_trsm[i].transform.rotation.y;
                x[t, i * 3 + 2] = x_trsm[i].transform.rotation.z;

                targs[t, i * 3] = target_trsm[i].transform.rotation.x;
                targs[t, i * 3 + 1] = target_trsm[i].transform.rotation.y;
                targs[t, i * 3 + 2] = target_trsm[i].transform.rotation.z;

            }
        }
        if (usingPast)
        {
            string[] lines = File.ReadAllLines(Application.dataPath + "/network_2.txt");
            string[] dlines = new string[CONTROL_JOINTS + 1];

            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                dlines[i] = lines[lines.Length - (CONTROL_JOINTS - i + 2)];
            }
            int j = 0;
            foreach (string lin in dlines)
            {
                string[] d = lin.Split(' ');

                int ind = 0;
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

                j++;
            }
        }
        else
        {
            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
                {
                    wf[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    wi[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    wc[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    wo[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                }


                uf[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                ui[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                uc[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                uo[i] = UnityEngine.Random.Range(-1.0f, 1.0f);

                bf[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                bi[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                bc[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                bo[i] = UnityEngine.Random.Range(-1.0f, 1.0f);

            }

        }

    }
    void FixedUpdate()
    {
        if (!written && Input.GetKeyDown(KeyCode.S))
        {
            writeStuff(false);
            written = true;
            Debug.Log("2 : WRITTEN");
        }
        if (frames == 28)
        {
            targBall.GetComponent<Rigidbody>().isKinematic = false;
            targBall.transform.SetParent(null);
            targInHand = false;
        }


        for (int i = 0; i < CONTROL_JOINTS / 3; i++)
        {
            if (frames >= FRAMES) break;

            
            x[frames, i * 3] = x_trsm[i].transform.rotation.x - rotPrevx[i * 3];
            x[frames, i * 3 + 1] = x_trsm[i].transform.rotation.y - rotPrevx[i * 3 + 1];
            x[frames, i * 3 + 2] = x_trsm[i].transform.rotation.z - rotPrevx[i * 3 + 2];

            targs[frames, i * 3] = target_trsm[i].transform.rotation.x - rotPrev[i * 3];
            targs[frames, i * 3 + 1] = target_trsm[i].transform.rotation.y - rotPrev[i * 3 + 1];
            targs[frames, i * 3 + 2] = target_trsm[i].transform.rotation.z - rotPrev[i * 3 + 2];

            rotPrevx[i * 3] = x_trsm[i].transform.rotation.x;
            rotPrevx[i * 3 + 1] = x_trsm[i].transform.rotation.y;
            rotPrevx[i * 3 + 2] = x_trsm[i].transform.rotation.z;
            rotPrev[i * 3] = target_trsm[i].transform.rotation.x;
            rotPrev[i * 3 + 1] = target_trsm[i].transform.rotation.y;
            rotPrev[i * 3 + 2] = target_trsm[i].transform.rotation.z;

            /*x[frames, i * 3] = x_trsm[i].transform.rotation.x;
            x[frames, i * 3 + 1] = x_trsm[i].transform.rotation.y;
            x[frames, i * 3 + 2] = x_trsm[i].transform.rotation.z;

            targs[frames, i * 3] = target_trsm[i].transform.rotation.x;
            targs[frames, i * 3 + 1] = target_trsm[i].transform.rotation.y;
            targs[frames, i * 3 + 2] = target_trsm[i].transform.rotation.z;*/

        }
        int join = 0;
        for (int i = 0; i < CONTROL_JOINTS; i++)
        {
            if (frames >= FRAMES) break;
            inputs[frames, i] = target_trsm[join].transform.rotation.x;
            inputs[frames, i + 1] = target_trsm[join].transform.rotation.y;
            inputs[frames, i + 2] = target_trsm[join].transform.rotation.z;
            i += 2;
            join++;

        }
        join = 0;
        for (int i = CONTROL_JOINTS; i < CONTROL_JOINTS * 2; i++)
        {
            if (frames >= FRAMES) break;
            inputs[frames, i] = target_trsm[join].transform.position.x;
            inputs[frames, i + 1] = target_trsm[join].transform.position.y;
            inputs[frames, i + 2] = target_trsm[join].transform.position.z;
            i += 2;
            join++;
        }
        join = 0;
        for (int i = CONTROL_JOINTS * 2; i < CONTROL_JOINTS * 3; i++)
        {
            if (frames >= FRAMES) break;
            inputs[frames, i] = targrbs[join].velocity.x;
            inputs[frames, i + 1] = targrbs[join].velocity.x;
            inputs[frames, i + 2] = targrbs[join].velocity.x;
            i += 2;
            join++;
        }
        join = 0;
        for (int i = CONTROL_JOINTS * 3; i < CONTROL_JOINTS * 4; i++)
        {
            if (frames >= FRAMES) break;
            inputs[frames, i] = targrbs[join].angularVelocity.x;
            inputs[frames, i + 1] = targrbs[join].angularVelocity.x;
            inputs[frames, i + 2] = targrbs[join].angularVelocity.x;
            i += 2;
            join++;
        }


        if (frames == 0)
        {
            anim.SetTrigger("pa");
            Debug.Log(test);
            //if (test != 50000 && test % 200 == 0 && gamma > 0.001) gamma /= 10;
        }
        if (frames < FRAMES)
        {

            inputs[frames, CONTROL_JOINTS * 4] = targBall.position.x;
            inputs[frames, CONTROL_JOINTS * 4 + 1] = targBall.position.y;
            inputs[frames, CONTROL_JOINTS * 4 + 2] = targBall.position.z;
            inputs[frames, CONTROL_JOINTS * 4 + 3] = targBall.GetComponent<Rigidbody>().velocity.x;
            inputs[frames, CONTROL_JOINTS * 4 + 4] = targBall.GetComponent<Rigidbody>().velocity.y;
            inputs[frames, CONTROL_JOINTS * 4 + 5] = targBall.GetComponent<Rigidbody>().velocity.z;
            inputs[frames, CONTROL_JOINTS * 4 + 6] = (targInHand ? 1 : 0);


            LSTM_Forward_Prop();
            count += calculate_error();

        }
        else if (frames > FRAMES + 3)
        {
            frames = -1;
            resetDelOut();
            test--;
            if (test % 200 == 0)
            {
                File.AppendAllText(Application.dataPath + "/errors.txt", "2 - test1: " + test + " error: " + count + "\n");
            }
            if (test % 1500 == 0)
            {
                //if (gamma > 0.004) gamma /= 2;
                writeStuff(false);
                /*for (int i = 0; i < CONTROL_JOINTS + 1; i++)
                {
                    for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
                    {
                        wf[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                        wi[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                        wc[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                        wo[i, j] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    }


                    uf[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    ui[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    uc[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    uo[i] = UnityEngine.Random.Range(-1.0f, 1.0f);

                    bf[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    bi[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    bc[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
                    bo[i] = UnityEngine.Random.Range(-1.0f, 1.0f);

                }
                File.AppendAllText(Application.dataPath + "/errors.txt", "2 - gamma: " + gamma + " error: " + count + "\n");
                gamma /= 1.5;*/

            }
            count = 0;
            inHand = true;
            targInHand = true;
            Physics.autoSimulation = true;
            alreadyDone = false;
        }
        else if (frames >= FRAMES && test > 0 && !alreadyDone)
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

        for (int i = 0; i < CONTROL_JOINTS + 1; i++)
        {
            summation = 0;
            for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
            {
                summation += (inputs[frames, j] * wf[i, j]);

            }
            if (frames != 0)
                forgetGate[frames, i] = sigma(summation + (finalOutput[frames - 1, i] * uf[i]) + bf[i]);
            else
                forgetGate[frames, i] = sigma(summation + bf[i]);

            //if (frames == 5) Debug.Log("forget: " + forgetGate[frames, i]);


        }

        // Calculate update layer
        for (int i = 0; i < CONTROL_JOINTS + 1; i++)
        {
            summation = 0;
            for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
            {
                summation += (inputs[frames, j] * wi[i, j]);

            }
            if (frames != 0)
                inputGate[frames, i] = sigma(summation + (finalOutput[frames - 1, i] * ui[i]) + bi[i]);
            else
                inputGate[frames, i] = sigma(summation + bi[i]);
            // if (frames == 5) Debug.Log("input: " + inputGate[frames, i]);

        }

        // Calculate candidate layer
        for (int i = 0; i < CONTROL_JOINTS + 1; i++)
        {
            summation = 0;
            for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
            {
                summation += (inputs[frames, j] * wc[i, j]);

            }
            if (frames != 0)
                updateCandidate[frames, i] = tanH(summation + (finalOutput[frames - 1, i] * uc[i]) + bc[i]);
            else
                updateCandidate[frames, i] = tanH(summation + bc[i]);
            //if (frames == 5) Debug.Log("updateCandidate: " + updateCandidate[frames, i]);
        }

        // Calculate output layer
        for (int i = 0; i < CONTROL_JOINTS + 1; i++)
        {
            summation = 0;
            for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
            {
                summation += (inputs[frames, j] * wo[i, j]);

            }
            if (frames != 0)
                outputCell[frames, i] = sigma(summation + (finalOutput[frames - 1, i] * uo[i]) + bo[i]);
            else
                outputCell[frames, i] = sigma(summation + bo[i]);

            //if (frames == 5) Debug.Log("outputGate: " + outputCell[frames, i]);
        }

        // Calculate ct-1
        for (int i = 0; i < CONTROL_JOINTS + 1; i++)
        {
            finalCandidate[frames, i] = (inputGate[frames, i] * updateCandidate[frames, i]);
            if (frames != 0) finalCandidate[frames, i] += (finalCandidate[frames - 1, i] * forgetGate[frames, i]);

            //if (frames == 5) Debug.Log("finalCandidate: " + finalCandidate[frames, i]);
        }

        // Calculate final output
        for (int i = 0; i < CONTROL_JOINTS + 1; i++)
        {
            finalOutput[frames, i] = outputCell[frames, i] * tanH(finalCandidate[frames, i]);
            //if (frames == 5) Debug.Log("output: " + finalOutput[frames, i]);

        }

        // Move the player
        for (int i = 0; i < CONTROL_JOINTS / 3; i++)
        {
            //rbs[i].AddTorque(finalOutput[frames, i * 3] * force, finalOutput[frames, i * 3 + 1] * force , finalOutput[frames, i * 3 + 2] * force);
            x_trsm[i].transform.Rotate((float)finalOutput[frames, i * 3] * 10, (float)finalOutput[frames, i * 3 + 1] * 10, (float)finalOutput[frames, i * 3 + 2] * 10, Space.Self);
        }

        if (finalOutput[frames, CONTROL_JOINTS] > 0 && inHand)
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
            MSE += Math.Pow((x[frames, i] - targs[frames, i]), 2);
            // MSE += Mathf.Pow(x[frames, i] - targs[frames, i], 2);
        }
        MSE *= (1.0f / CONTROL_JOINTS);

        //Debug.Log("MSE: " + MSE.ToString());

        return MSE;
    }

    void LSTM_Back_Prop()
    {
        int fr = FRAMES - 1;
        while (fr >= 0)
        {
            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                if (i == CONTROL_JOINTS) errorDerv[fr, i] = (inHand == targInHand ? 0 : 1);
                else errorDerv[fr, i] = (x[fr, i] - targs[fr, i]);
                //errorDerv[fr, i] = (x[fr, i] - targs[fr, i]);

                dOut[fr, i] = errorDerv[fr, i];
                if (fr != FRAMES - 1)
                    dOut[fr, i] += del_out[fr + 1, i];

                dFinalCandidate[fr, i] = dOut[fr, i] * outputCell[fr, i] * (1 - tanH(finalCandidate[fr, i]) * tanH(finalCandidate[fr, i]));

                if (fr != FRAMES - 1)
                    dFinalCandidate[fr, i] += (dFinalCandidate[fr + 1, i] * forgetGate[fr + 1, i]);

                /*if (fr == 5) Debug.Log("errorDerv " + errorDerv[fr, i]);
                if (fr == 5) Debug.Log("dOut " + dOut[fr, i]);
                if (fr == 5) Debug.Log("dFinalCandidate " + dFinalCandidate[fr, i]);*/


            }

            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {
                dUpdateCandidate[fr, i] = dFinalCandidate[fr, i] * inputGate[fr, i] * (1 - updateCandidate[fr, i] * updateCandidate[fr, i]);

                dInput[fr, i] = dFinalCandidate[fr, i] * updateCandidate[fr, i] * inputGate[fr, i] * (1 - inputGate[fr, i]);

                if (fr > 0)
                    dForget[fr, i] = dFinalCandidate[fr, i] * finalCandidate[fr - 1, i] * forgetGate[fr, i] * (1 - forgetGate[fr, i]);
                else dForget[fr, i] = 0.0f;

                dOutputCell[fr, i] = dOut[fr, i] * tanH(finalCandidate[fr, i]) * outputCell[fr, i] * (1 - outputCell[fr, i]);

                /*if (fr == 5) Debug.Log("dUpdateCandidate " + dUpdateCandidate[fr, i]);
                if (fr == 5) Debug.Log("dInput " + dInput[fr, i]);
                if (fr == 5) Debug.Log("dForget " + dForget[fr, i]);
                if (fr == 5) Debug.Log("dOutputGate " + dOutputCell[fr, i]);*/



            }

            for (int i = 0; i < CONTROL_JOINTS + 1; i++)
            {

                del_out[fr, i] += uc[i] * dUpdateCandidate[fr, i];
                del_out[fr, i] += ui[i] * dInput[fr, i];
                del_out[fr, i] += uf[i] * dForget[fr, i];
                del_out[fr, i] += uo[i] * dOutputCell[fr, i];

            }
            fr--;
        }

        for (int t = 0; t < FRAMES; t++)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < CONTROL_JOINTS + 1; j++)
                {
                    switch (i)
                    {
                        case 0:
                            dB[i, j] += dUpdateCandidate[t, j];
                            break;
                        case 1:
                            dB[i, j] += dInput[t, j];
                            break;
                        case 2:
                            dB[i, j] += dForget[t, j];
                            break;
                        case 4:
                            dB[i, j] += dOutputCell[t, j];
                            break;
                    }


                    for (int k = 0; k < CONTROL_JOINTS * 4 + 7; k++)
                        switch (i)
                        {
                            case 0:
                                dW[i, j, k] += inputs[t, k] * dUpdateCandidate[t, j];
                                break;
                            case 1:
                                dW[i, j, k] += inputs[t, k] * dInput[t, j];

                                break;
                            case 2:
                                dW[i, j, k] += inputs[t, k] * dForget[t, j];
                                break;
                            case 3:
                                dW[i, j, k] += inputs[t, k] * dOutputCell[t, j];
                                break;
                        }
                }
            }
        }
        for (int t = 0; t < FRAMES - 1; t++)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < CONTROL_JOINTS + 1; j++)
                {
                    switch (i)
                    {
                        case 0:
                            dU[i, j] += finalOutput[t, j] * dUpdateCandidate[t + 1, j];
                            break;
                        case 1:
                            dU[i, j] += finalOutput[t, j] * dInput[t + 1, j];
                            break;
                        case 2:
                            dU[i, j] += finalOutput[t, j] * dForget[t + 1, j];
                            break;
                        case 3:
                            dU[i, j] += finalOutput[t, j] * dOutputCell[t + 1, j];
                            break;
                    }
                }
            }
        }

        // update parameters


        for (int i = 0; i < 4; i++)
        {

            for (int j = 0; j < CONTROL_JOINTS + 1; j++)
            {
                switch (i)
                {
                    case 0:
                        uc[j] -= dU[i, j] * gamma;
                        bc[j] -= dB[i, j] * gamma;
                        break;
                    case 1:
                        ui[j] -= dU[i, j] * gamma;
                        bi[j] -= dB[i, j] * gamma;
                        break;
                    case 2:
                        uf[j] -= dU[i, j] * gamma;
                        bf[j] -= dB[i, j] * gamma;

                        break;
                    case 3:
                        uo[j] -= dU[i, j] * gamma;
                        bo[j] -= dB[i, j] * gamma;
                        break;
                }
                for (int k = 0; k < CONTROL_JOINTS * 4 + 7; k++)
                    switch (i)
                    {
                        case 0:
                            wc[j, k] -= dW[i, j, k] * gamma;
                            break;
                        case 1:
                            wi[j, k] -= dW[i, j, k] * gamma;
                            break;
                        case 2:
                            wf[j, k] -= dW[i, j, k] * gamma;

                            break;
                        case 3:
                            wo[j, k] -= dW[i, j, k] * gamma;
                            break;
                    }
            }
        }
    }

    void resetPlayer()
    {
        Destroy(ball.gameObject);
        Destroy(player);
        Destroy(targBall.gameObject);
        targBall = GameObject.Instantiate(prefab_ball, target_trsm[16]).transform;
        player = GameObject.Instantiate(playerCopy, this.transform);
        player.SetActive(true);
        setRbsAndTrsfs();
    }

    double sigma(double x)
    {
        /*Debug.Log("SIGMA: " + (1.0f / (1.0f + Mathf.Exp(-x))));*/

        // So that it doesn't divide by 0;
        if (Double.IsNaN(1.0f / (1.0f + Math.Exp(-x)))) return (0.0f);
        else return 1.0f / (1.0f + Math.Exp(-x));
    }

    double tanH(double x)
    {
        /* Debug.Log("TANH: " + ((Mathf.Exp(x) - Mathf.Exp(-x)) / (Mathf.Exp(x) + Mathf.Exp(-x))));*/

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

        target_trsm[0] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT");
        target_trsm[1] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/l_upleg_JNT");
        target_trsm[2] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/l_upleg_JNT/l_leg_JNT");
        target_trsm[3] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/l_upleg_JNT/l_leg_JNT/l_foot_JNT/l_toebase_JNT");
        target_trsm[4] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/r_upleg_JNT");
        target_trsm[5] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/r_upleg_JNT/r_leg_JNT");
        target_trsm[6] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/r_upleg_JNT/r_leg_JNT/r_foot_JNT/r_toebase_JNT");
        target_trsm[7] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT");
        target_trsm[8] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT");
        target_trsm[9] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT");
        target_trsm[10] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT");
        target_trsm[11] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT/l_handMiddle1_JNT/l_handMiddle2_JNT");
        target_trsm[12] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/neck_JNT");
        target_trsm[13] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT");
        target_trsm[14] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT");
        target_trsm[15] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT");
        target_trsm[16] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT");
        if (target_trsm[16] == null) Debug.Log("BRRUUUUH");


        targrbs[0] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT").GetComponent<Rigidbody>();
        targrbs[1] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/l_upleg_JNT").GetComponent<Rigidbody>();
        targrbs[2] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/l_upleg_JNT/l_leg_JNT").GetComponent<Rigidbody>();
        targrbs[3] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/l_upleg_JNT/l_leg_JNT/l_foot_JNT/l_toebase_JNT").GetComponent<Rigidbody>();
        targrbs[4] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/r_upleg_JNT").GetComponent<Rigidbody>();
        targrbs[5] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/r_upleg_JNT/r_leg_JNT").GetComponent<Rigidbody>();
        targrbs[6] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/r_upleg_JNT/r_leg_JNT/r_foot_JNT/r_toebase_JNT").GetComponent<Rigidbody>();
        targrbs[7] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT").GetComponent<Rigidbody>();
        targrbs[8] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT").GetComponent<Rigidbody>();
        targrbs[9] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT").GetComponent<Rigidbody>();
        targrbs[10] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT").GetComponent<Rigidbody>();
        targrbs[11] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/l_shoulder_JNT/l_arm_JNT/l_forearm_JNT/l_hand_JNT/l_handMiddle1_JNT/l_handMiddle2_JNT").GetComponent<Rigidbody>();
        targrbs[12] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/neck_JNT").GetComponent<Rigidbody>();
        targrbs[13] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT").GetComponent<Rigidbody>();
        targrbs[14] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT").GetComponent<Rigidbody>();
        targrbs[15] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT").GetComponent<Rigidbody>();
        targrbs[16] = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT").GetComponent<Rigidbody>();

        ball = player.transform.Find("Slash_Young_Male.001/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT/Ball");
        targBall = perfect.transform.Find("throw2_customModel_mfG4iCwRbRee1GxaHvYqr3/hips_JNT/spine_JNT/spine1_JNT/spine2_JNT/r_shoulder_JNT/r_arm_JNT/r_forearm_JNT/r_hand_JNT/r_handMiddle1_JNT/r_handMiddle2_JNT/Ball");

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
        for (int i = 0; i < CONTROL_JOINTS + 1; i++)
        {
            for (int j = 0; j < CONTROL_JOINTS * 4 + 7; j++)
            {
                s += wf[i, j] + " " + wi[i, j] + " " + wc[i, j] + " " + wo[i, j] + " ";
            }
            s += uf[i] + " " + ui[i] + " " + uc[i] + " " + +uo[i] + " " + bf[i] + " " + bi[i] + " " + bc[i] + " " + bo[i] + "\n";
        }
        s += "\n";
        File.AppendAllText(Application.dataPath + "/network_2.txt", s);



    }
    void resetDelOut()
    {

        for (int i = 0; i < FRAMES; i++)
        {

            for (int j = 0; j < CONTROL_JOINTS + 1; j++)
            {
                del_out[i, j] = 0.0f;

            }
        }
        for (int i = 0; i < CONTROL_JOINTS; i++)
        {
            rotPrevx[i] = 0.0f;
            rotPrev[i] = 0.0f;
        }
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < CONTROL_JOINTS + 1; j++)
            {
                for (int k = 0; k < CONTROL_JOINTS * 4 + 7; k++)
                    dW[i, j, k] = 0.0f;
                dU[i, j] = 0.0f;
                dB[i, j] = 0.0f;
            }
        }
    }
    /* void setPositions() {
         for (int i = 0; i < CONTROL_JOINTS/3; i++) {
             x_trsm[i] = target_trsm[i];
         }


     }*/
}



