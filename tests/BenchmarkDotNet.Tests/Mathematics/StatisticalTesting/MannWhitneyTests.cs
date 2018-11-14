using BenchmarkDotNet.Mathematics.StatisticalTesting;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mathematics.StatisticalTesting
{
    public class MannWhitneyTests
    {
        private readonly ITestOutputHelper output;

        public MannWhitneyTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(-30, 91, 0.0005250168)]
        [InlineData(-15, 76, 0.02621295)]
        [InlineData(0, 47, 0.6020319)]
        [InlineData(15, 25, 0.973787)]
        [InlineData(30, 12, 0.9989554)]
        public void N10(double t, int u, double pValue)
        {
            double[] x =
            {
                101.560680277179, 84.5009342862592, 87.2294917081476, 129.63805718437, 105.404085588131, 107.725754537927, 80.1006409348006, 113.864283866225,
                102.430864178515, 104.403374809552
            };
            double[] y =
            {
                82.352823982586, 126.135642815364, 109.069571619921, 91.5330798762612, 94.2532257217699, 70.568270818608, 92.5436886596998, 117.210691979832,
                119.721076854462, 113.813867732564
            };

            Check(x, y, new AbsoluteThreshold(t), u, pValue);
        }

        [Theory]
        [InlineData(-10, 242, 0.1324159)]
        [InlineData(-5, 197, 0.5372871)]
        [InlineData(0, 163, 0.8429579)]
        [InlineData(5, 123, 0.9824947)]
        [InlineData(10, 88, 0.9991166)]
        public void N20(double t, int u, double pValue)
        {
            double[] x =
            {
                97.8506464838511, 90.5704863589574, 106.919850380017, 114.968781001117, 133.163509926947, 98.5371366517863, 89.9809819778675, 94.8330330098023,
                113.848103602446, 100.747077722794, 118.58905185697, 100.050778406168, 103.429498836193, 91.9991877755192, 117.868745215013, 107.662781698883,
                110.434016813579, 118.772597143401, 91.0851351093108, 108.595287565388
            };
            double[] y =
            {
                141.325356080042, 101.705375558118, 121.135232885412, 104.233257876038, 113.697498891312, 89.3279837525589, 112.305397095148, 107.035411938303,
                126.862312493224, 137.838076178722, 100.055966433672, 106.011372282587, 99.220787317467, 105.141703581153, 95.2136664971929, 127.87168139858,
                97.5524729768301, 117.961786925524, 142.32225043168, 85.9508305239065
            };

            Check(x, y, new AbsoluteThreshold(t), u, pValue);
        }

        [Theory]
        [InlineData(-75, 947, 4.162974e-11)]
        [InlineData(-50, 549, 0.3131681)]
        [InlineData(-25, 119, 1)]
        [InlineData(0, 5, 1)]
        [InlineData(25, 0, 1)]
        public void N32(double t, int u, double pValue)
        {
            double[] x =
            {
                116.998310570223, 103.536732200858, 81.4977149674052, 109.240370046523, 112.685608892842, 92.4961887142488, 81.0875839754617, 107.885504895313,
                129.489136448277, 114.743862868848, 92.8640399750995, 99.5044810768261, 100.611715633651, 92.9347711165703, 125.105417118737, 107.956368991103,
                104.812061571297, 93.8558811186138, 101.649455913474, 108.168151882154, 86.9840546937499, 102.813126398294, 62.9278860673337, 81.9721302396966,
                99.5326103757994, 99.1127497773892, 89.6472790590878, 94.5383623495609, 104.265979458838, 89.0962721023424, 113.828847763463, 74.9119601526303
            };
            double[] y =
            {
                134.807641760747, 153.862779834471, 155.685166413893, 153.130746875771, 153.260935827381, 156.349904137714, 156.189659805232, 121.163530829502,
                147.486553850055, 132.381319853769, 135.272938963356, 159.631552618188, 165.266283582675, 150.072225393336, 150.645061760864, 139.783511337841,
                144.258022179319, 173.127172505296, 168.767084858625, 138.364489170039, 156.172173370812, 125.970596582247, 152.637584228714, 147.006073519481,
                134.162738650882, 138.128111355384, 163.306156590866, 133.37466051387, 141.733740469802, 153.328995694082, 154.841803368071, 120.244223151518
            };

            Check(x, y, new AbsoluteThreshold(t), u, pValue);
        }

        [Theory]
        [InlineData(-25, 1089, 1.385151e-19)]
        [InlineData(0, 1082, 6.23318e-18)]
        [InlineData(25, 953, 9.170051e-09)]
        [InlineData(50, 510, 0.6721925)]
        [InlineData(75, 112, 1)]
        public void N33(double t, int u, double pValue)
        {
            double[] x =
            {
                157.183612367683, 152.028902955382, 153.698861602206, 153.609486299015, 154.291063026817, 141.317831985261, 178.900284004259, 167.478597683646,
                161.832190643462, 147.626654232355, 148.374015299603, 174.068375692261, 141.51911277841, 139.69371979521, 126.871207404749, 153.808773375195,
                161.106156148851, 155.51480817283, 157.155363595268, 158.055454232685, 162.798646844568, 166.532006705421, 140.111049964116, 128.996157091284,
                169.680723418997, 137.479319232307, 157.03932404715, 163.508869366932, 165.356386977935, 145.571530643651, 155.600613010786, 158.004851800436,
                134.918152600764
            };
            double[] y =
            {
                99.9917764157985, 110.338659968681, 91.9010057855087, 120.039901613317, 117.797945862094, 123.134528084071, 72.8122128513182, 97.7368150018351,
                113.098574902131, 97.3639348027811, 110.481300557315, 140.288931294698, 102.184946433365, 97.0400366425305, 98.0057356321133, 103.417878439606,
                99.4765968818225, 79.2430526140985, 75.375686521915, 104.855304104658, 103.619087606989, 96.227750057318, 112.563251473402, 82.063032157974,
                126.197302954924, 81.2530975187152, 124.059305772757, 117.114945385568, 109.651754612905, 103.601482510961, 128.404461213919, 123.245412480788,
                121.186685876309
            };

            Check(x, y, new AbsoluteThreshold(t), u, pValue);
        }

        [Theory]
        [InlineData(-2, 897, 0.0001587151)]
        [InlineData(-1.5, 765, 0.02523963)]
        [InlineData(-1, 628, 0.3720417)]
        [InlineData(-0.5, 489, 0.906506)]
        [InlineData(0, 359, 0.9981441)]
        [InlineData(0.5, 261, 0.9999845)]
        public void N30Vs40(double t, int u, double pValue)
        {
            // set.seed(42); x <- rnorm(30, mean = 10)
            // set.seed(42); y <- rnorm(40, mean = 11)
            double[] x =
            {
                11.3709584471467, 9.43530182860391, 10.3631284113373, 10.632862604961,
                10.404268323141, 9.89387548390852, 11.5115219974389, 9.9053409615869,
                12.018423713877, 9.93728590094758, 11.3048696542235, 12.2866453927011,
                8.61113929888766, 9.72121123318263, 9.86667866360634, 10.6359503980701,
                9.71574707858393, 7.34354457909522, 7.55953307142448, 11.3201133457302,
                9.69336140592153, 8.21869156602, 9.82808264424038, 11.2146746991726,
                11.895193461265, 9.5695308683938, 9.74273061723107, 8.23683691480522,
                10.4600973548313, 9.36000512403988
            };
            double[] y =
            {
                12.3709584471467, 10.4353018286039, 11.3631284113373, 11.632862604961,
                11.404268323141, 10.8938754839085, 12.5115219974389, 10.9053409615869,
                13.018423713877, 10.9372859009476, 12.3048696542235, 13.2866453927011,
                9.61113929888766, 10.7212112331826, 10.8666786636063, 11.6359503980701,
                10.7157470785839, 8.34354457909522, 8.55953307142448, 12.3201133457302,
                10.6933614059215, 9.21869156602, 10.8280826442404, 12.2146746991726,
                12.895193461265, 10.5695308683938, 10.7427306172311, 9.23683691480522,
                11.4600973548313, 10.3600051240399, 11.4554501232412, 11.7048373372288,
                12.0351035219699, 10.3910736245928, 11.504955123298, 9.28299132092666,
                10.2155409916205, 10.1490924058235, 8.58579235005337, 11.0361226068923
            };
            Check(x, y, new AbsoluteThreshold(t), u, pValue);
        }

        [Theory]
        [InlineData(0, 63920, 12.521533e-19)]
        [InlineData(0.5, 56853.5, 1.182653e-08)]
        [InlineData(1, 45897.5, 0.3363308)]
        [InlineData(1.1, 43563, 0.7508224)]
        [InlineData(2, 29397, 1)]
        public void N300(double t, double u, double pValue)
        {
            double[] x =
            {
                353.4302, 350.9193, 396.4676, 354.2004, 349.9472, 350.1746, 351.1972, 361.5375, 357.1806, 355.6957, 350.9530, 353.7620, 350.9252, 350.2909,
                357.2528, 350.4599, 350.7203, 351.4393, 349.4832, 348.9141, 350.8004, 349.2567, 348.6628, 348.8442, 350.8513, 355.6557, 348.9383, 349.2411,
                348.5117, 349.9738, 349.5833, 350.9835, 348.5326, 348.9863, 349.5978, 352.2430, 353.9921, 348.7386, 353.8779, 349.3807, 348.6024, 348.7450,
                348.7609, 348.5573, 351.4074, 348.7916, 348.9138, 349.4394, 355.9450, 351.0741, 350.4301, 351.6454, 348.8297, 348.4810, 348.9067, 348.8078,
                351.3754, 348.2212, 350.6068, 351.8590, 349.4409, 353.8832, 352.1167, 351.4719, 348.9147, 350.5190, 350.8566, 348.5493, 348.3130, 354.7536,
                348.5446, 349.7245, 349.3147, 349.2842, 351.3039, 351.6084, 349.1143, 348.4140, 350.6863, 355.6851, 348.4323, 348.6842, 348.9533, 353.1712,
                350.9491, 353.3528, 350.9518, 352.7969, 348.3916, 355.0802, 348.2997, 348.7019, 348.7373, 348.3435, 348.3017, 351.7710, 353.3918, 349.3426,
                349.3856, 348.9890, 348.6114, 352.0596, 348.4195, 357.1810, 350.6999, 349.2421, 350.7046, 349.0863, 350.1624, 350.1976, 348.3483, 348.4665,
                352.4801, 348.7607, 350.7053, 349.3357, 352.0602, 348.2372, 349.0550, 348.8323, 348.3879, 349.4077, 348.6808, 348.4246, 348.4569, 349.1250,
                348.9443, 348.9642, 348.3971, 356.3534, 348.2787, 349.8091, 348.5167, 348.4438, 348.3847, 348.5627, 348.6409, 348.3877, 352.1406, 348.4861,
                348.7049, 350.7922, 352.7203, 348.9340, 348.4139, 349.4621, 348.8676, 349.7629, 350.9608, 348.2334, 348.8159, 348.5077, 348.1488, 348.4427,
                348.4489, 348.6434, 348.7352, 348.5058, 348.3966, 353.2068, 352.4687, 349.0576, 349.9138, 350.8908, 349.1250, 351.0939, 351.1674, 348.2952,
                348.6345, 348.3658, 354.7745, 348.0121, 349.5867, 350.2797, 348.4887, 348.4441, 374.9498, 348.2829, 349.4644, 349.5089, 349.8994, 349.0551,
                351.5127, 351.2810, 351.3763, 349.8671, 349.1344, 349.1842, 349.9880, 353.1967, 350.0169, 351.4005, 364.2627, 351.5116, 350.4371, 349.6280,
                354.5077, 351.2293, 351.2565, 349.7923, 351.1246, 351.4023, 350.9489, 373.6979, 348.9797, 349.4231, 351.6052, 350.3881, 372.3069, 348.8843,
                355.4068, 350.8477, 350.6771, 348.3057, 350.2932, 348.4213, 349.1609, 348.3758, 350.7920, 350.6115, 350.7276, 349.8305, 348.2517, 349.7037,
                348.3101, 349.3411, 348.5030, 356.8544, 348.9204, 350.1835, 348.1639, 349.5208, 348.4460, 348.4046, 349.9324, 348.5018, 348.6524, 349.5684,
                348.4567, 348.6487, 348.3740, 348.2163, 348.5270, 358.3175, 353.3845, 349.2999, 348.9381, 349.1241, 348.6709, 348.4846, 357.6281, 348.9662,
                348.9404, 348.8550, 348.5498, 349.3227, 348.6938, 353.9863, 349.5527, 349.7617, 349.0910, 348.6906, 351.2841, 349.5185, 350.0456, 349.2823,
                351.1163, 354.7285, 350.5538, 353.3063, 349.2634, 348.4159, 348.4064, 360.4929, 350.9462, 348.4895, 348.3143, 349.4910, 352.0636, 352.6701,
                348.6482, 348.3380, 348.3338, 354.2525, 371.4057, 348.8699, 349.3964, 352.7111, 352.3196, 349.5104, 348.7168, 356.8885, 352.2493, 348.8851,
                352.0130, 349.3317, 351.2397, 348.4217, 351.9049, 348.6496
            };
            double[] y =
            {
                352.6772, 352.0694, 351.7422, 353.0724, 351.2761, 355.6030, 351.3673, 351.6215, 351.0600, 353.0083, 351.2111, 352.7424, 350.7463, 350.8995,
                350.9788, 351.5278, 347.2926, 347.2408, 347.3430, 350.2349, 348.9812, 349.5674, 348.9471, 351.0041, 350.2227, 350.8498, 350.5350, 351.6645,
                352.8947, 347.5184, 347.7925, 347.5337, 347.7620, 347.8514, 351.8277, 347.6886, 353.8707, 347.7313, 348.5841, 348.9841, 347.5774, 347.9248,
                348.3379, 347.9148, 347.5209, 350.3050, 348.2506, 352.2775, 349.0242, 347.6859, 347.9177, 348.2372, 347.6983, 347.9181, 362.5876, 349.5010,
                347.3098, 347.7068, 347.6530, 348.8905, 349.1875, 347.7322, 347.8935, 350.1428, 347.8078, 347.4217, 347.9083, 351.6060, 364.3534, 362.0277,
                347.6155, 348.8701, 350.0820, 350.4459, 347.7448, 347.9179, 348.2846, 357.5326, 348.5791, 348.1976, 348.2805, 347.8833, 351.8235, 348.4769,
                348.1972, 354.2277, 348.7812, 347.5858, 347.5523, 347.9894, 348.4267, 347.7855, 352.2337, 348.1318, 347.5751, 348.5764, 347.8220, 351.9602,
                347.4684, 348.3530, 348.3222, 348.4553, 347.7500, 347.9536, 348.3003, 348.6743, 350.6669, 348.7904, 348.7561, 349.0508, 347.6311, 348.3785,
                348.3788, 352.3984, 347.4498, 349.6662, 349.0177, 347.5398, 355.2611, 347.6171, 349.0511, 347.3273, 347.3592, 347.8611, 347.5964, 347.5564,
                347.6554, 348.2081, 348.2998, 347.7288, 347.8346, 347.7421, 352.2372, 348.1965, 349.0271, 347.9774, 351.8596, 347.5298, 347.6308, 347.6561,
                347.8762, 347.3469, 347.6736, 352.0812, 349.5322, 349.2015, 348.5227, 352.9716, 347.7573, 353.2357, 348.8211, 347.6180, 349.7692, 348.1887,
                347.7775, 348.8799, 351.0806, 349.5768, 357.9883, 348.7598, 351.0769, 347.7897, 349.8290, 349.0401, 349.8934, 347.3818, 347.4127, 347.2534,
                347.2993, 347.3662, 348.7725, 349.3746, 347.6021, 348.2902, 348.5113, 347.4069, 349.7877, 351.7751, 351.3461, 347.1249, 349.4440, 348.1466,
                347.3497, 347.8770, 350.7545, 349.9675, 347.6966, 347.6298, 349.0210, 347.7346, 347.1405, 349.4634, 348.7612, 352.6694, 349.1828, 348.0399,
                347.9772, 348.2017, 348.2891, 355.4806, 347.5127, 348.5503, 347.4944, 349.2060, 347.8054, 351.7816, 353.4037, 347.9604, 350.0297, 347.5814,
                347.5632, 347.8805, 348.2123, 348.7926, 347.5597, 347.5986, 352.1924, 374.1594, 349.3394, 349.7771, 347.5244, 348.7254, 356.8709, 348.2475,
                347.4074, 351.4388, 348.5651, 347.5950, 347.9851, 350.7727, 353.7092, 347.5937, 347.4309, 347.8552, 350.4125, 350.6890, 350.5143, 347.3917,
                348.2776, 354.7649, 349.4661, 347.4174, 348.2034, 350.1242, 347.3456, 347.8254, 351.1231, 347.5047, 347.4512, 352.3139, 347.5133, 348.6214,
                347.2056, 349.9073, 347.4756, 348.4515, 348.0344, 348.9201, 350.7587, 351.4907, 350.3649, 352.5194, 348.8017, 348.0644, 349.5104, 348.0675,
                347.8671, 351.0880, 349.6343, 347.5890, 347.6001, 348.6831, 348.7830, 348.0030, 348.3257, 347.6348, 347.3166, 347.3369, 349.0966, 348.0506,
                355.5348, 347.4069, 348.8350, 348.2581, 347.2280, 347.5418, 348.6233, 348.1083, 347.7203, 348.0728, 347.4089, 347.5658, 347.6575, 347.2848,
                347.4562, 347.3834, 349.0502, 350.6191, 347.4489, 348.6910
            };

            Check(x, y, new AbsoluteThreshold(t), u, pValue);
        }

        [Theory]
        [InlineData(0.1, 0, 0)] //I don't know u and pValue values
        public void Issue_948(double t, double u, double pValue)
        {
            double[] x =
            {
                117.428150690794, 114.372210364342, 113.014660415649, 112.944919220209, 112.383144277334, 112.360203094482, 111.301456588507, 110.338494982719,
                116.107656214237, 115.510661094189, 114.018304386139, 118.674752711058, 110.82060939908, 110.29270001173, 111.751715534925, 112.928226778507,
                109.582877969742, 112.403332518339, 115.566637580395
            };
            double[] y =
            {
                87.7416295671463, 88.7655491733551, 88.2931792974472, 88.481253298521, 90.3308806920052, 90.3109546351433, 89.6487628591061, 88.9729374647141,
                89.3410451281071, 88.1987927174568, 88.7037609219551, 89.2515526676178, 87.8502615296841, 87.7515925955772
            };

            Check(x, y, new RelativeThreshold(t), u, pValue);
        }

        [AssertionMethod]
        private void Check(double[] x, double[] y, Threshold t, double u, double pValue)
        {
            var result = MannWhitneyTest.Instance.IsGreater(x, y, t);
            output.WriteLine("Ux      = " + result.Ux);
            output.WriteLine("Uy      = " + result.Uy);
            output.WriteLine("p-value = " + result.PValue);
            output.WriteLine("H0      = " + result.H0);
            output.WriteLine("H1      = " + result.H1);

            Assert.Equal(u, result.Ux, 2);
            Assert.Equal(pValue, result.PValue, 2);

            Assert.Equal(x.Length * y.Length, result.Ux + result.Uy);
        }
    }
}