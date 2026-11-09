using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace OmiLAXR.Benchmark.Tests
{
    public class Benchmark_Tests
    {
        private GameObject _benchmarkObject;
        private Benchmark _benchmark;
        private int _vSyncCount;
        private int _targetFrameRate;

        [SetUp]
        public void SetUp()
        {
            // Benchmark.Awake changes global frame settings, restore them afterwards.
            _vSyncCount = QualitySettings.vSyncCount;
            _targetFrameRate = Application.targetFrameRate;

            _benchmarkObject = new GameObject("Benchmark");
            _benchmarkObject.SetActive(false);
            _benchmark = _benchmarkObject.AddComponent<Benchmark>();
            _benchmark.autoStartOnEnable = false;
            _benchmark.writeLogFile = false;
            _benchmark.writeCsv = false;
            _benchmarkObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_benchmarkObject);
            QualitySettings.vSyncCount = _vSyncCount;
            Application.targetFrameRate = _targetFrameRate;
        }

        private static void ExpectReport(string pattern)
            => LogAssert.Expect(LogType.Log, new Regex(pattern));

        [Test]
        public void Benchmark_Tests_Becomes_Instance_And_Sets_Frame_Settings()
        {
            // Assert
            Assert.AreSame(_benchmark, Benchmark.Instance);
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.AreEqual(120, Application.targetFrameRate);
        }

        [Test]
        public void Benchmark_Tests_Records_Custom_Metrics()
        {
            // Arrange
            ExpectReport("- Work: n=2, avg=[0-9.,]+ms, min=[0-9.,]+ms, max=[0-9.,]+ms");

            // Act
            Benchmark.StartRecord("Work");
            Benchmark.StopRecord("Work");
            Benchmark.StartRecord("Work");
            Benchmark.StopRecord("Work");
            Benchmark.DumpAll();
        }

        [Test]
        public void Benchmark_Tests_Unnamed_Metric_And_Empty_Session()
        {
            // Arrange
            ExpectReport("- Unnamed: n=1, avg=");

            // Act
            Benchmark.StartRecord(" ");
            Benchmark.StopRecord(null);
            Benchmark.DumpAll();
        }

        [Test]
        public void Benchmark_Tests_Warns_On_Invalid_Record_Calls()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, new Regex("StopRecord\\(\"Missing\"\\) called but metric is not running"));
            LogAssert.Expect(LogType.Warning, new Regex("StartRecord\\(\"Twice\"\\) called while already running"));

            // Act
            Benchmark.StopRecord("Missing");
            Benchmark.StartRecord("Twice");
            Benchmark.StartRecord("Twice");
        }

        [Test]
        public void Benchmark_Tests_ResetAll_Clears_Metrics()
        {
            // Arrange
            ExpectReport("Custom metrics: none recorded.");
            Benchmark.StartRecord("Work");
            Benchmark.StopRecord("Work");

            // Act
            Benchmark.ResetAll();
            Benchmark.DumpAll();
        }

        [Test]
        public void Benchmark_Tests_DumpAll_Invokes_Stop_Hook_And_Reports_Endpoints()
        {
            // Arrange
            var stopped = 0;
            _benchmark.onTrackingStop = new UnityEngine.Events.UnityEvent();
            _benchmark.onTrackingStop.AddListener(() => stopped++);
            var endpointObject = new GameObject("Endpoint");
            endpointObject.SetActive(false);
            endpointObject.AddComponent<CountingEndpoint>();
            ExpectReport("- Endpoint: CountingEndpoint recorded 0");

            // Act
            Benchmark.DumpAll();

            // Assert
            Assert.AreEqual(1, stopped);
            Object.DestroyImmediate(endpointObject);
        }

        [UnityTest]
        public IEnumerator Benchmark_Tests_Second_Instance_Is_Destroyed()
        {
            // Act
            var other = new GameObject("Other Benchmark");
            other.AddComponent<Benchmark>().autoStartOnEnable = false;
            yield return null;

            // Assert
            Assert.IsTrue(other == null);
            Assert.AreSame(_benchmark, Benchmark.Instance);
        }
    }

    public class StressScenario_Tests
    {
        private GameObject _scenarioObject;
        private StressScenario _scenario;

        [SetUp]
        public void SetUp()
        {
            _scenarioObject = new GameObject("Stress Scenario");
            _scenarioObject.SetActive(false);
            _scenario = _scenarioObject.AddComponent<StressScenario>();
            _scenario.spawnOnStart = false;
            _scenario.createPrimitives = false;
            _scenario.objectCount = 10;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_scenarioObject);
        }

        [Test]
        public void StressScenario_Tests_Spawns_Requested_Objects_As_Children()
        {
            // Act
            _scenario.Respawn();

            // Assert
            Assert.AreEqual(10, _scenarioObject.transform.childCount);
            Assert.AreEqual("StressObj_00000", _scenarioObject.transform.GetChild(0).name);
            Assert.AreEqual(Vector3.one * _scenario.uniformScale, _scenarioObject.transform.GetChild(0).localScale);
        }

        [Test]
        public void StressScenario_Tests_Layout_Is_Deterministic_And_Unique()
        {
            // Act
            _scenario.Respawn();
            var positions = Enumerable.Range(0, 10)
                .Select(i => _scenarioObject.transform.GetChild(i).localPosition).ToArray();

            // Assert
            Assert.AreEqual(10, positions.Distinct().Count());
        }

        [Test]
        public void StressScenario_Tests_Uses_Prefab_And_Disables_Collider()
        {
            // Arrange
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _scenario.primitivePrefab = prefab;
            _scenario.createPrimitives = true;
            _scenario.disableRenderer = true;
            _scenario.objectCount = 2;

            // Act
            _scenario.Respawn();

            // Assert
            var spawned = _scenarioObject.transform.GetChild(0);
            Assert.IsFalse(spawned.GetComponent<Collider>().enabled);
            Assert.IsFalse(spawned.GetComponent<MeshRenderer>().enabled);
            Object.DestroyImmediate(prefab);
        }

        [UnityTest]
        public IEnumerator StressScenario_Tests_Clear_Destroys_Objects()
        {
            // Arrange
            _scenario.Respawn();

            // Act
            _scenario.Clear();
            yield return null;

            // Assert
            Assert.AreEqual(0, _scenarioObject.transform.childCount);
        }

        [UnityTest]
        public IEnumerator StressScenario_Tests_Emits_Synthetic_Events_And_Moves_Objects()
        {
            // Arrange
            _scenario.objectCount = 1;
            _scenario.syntheticEventsPerSecond = 100000f;
            var events = 0;
            _scenario.onSyntheticEvent = new UnityEngine.Events.UnityEvent<int, float>();
            _scenario.onSyntheticEvent.AddListener((_, __) => events++);
            _scenario.Respawn();
            var basePosition = _scenarioObject.transform.GetChild(0).localPosition;
            _scenarioObject.SetActive(true);

            // Act
            yield return null;
            yield return null;

            // Assert
            Assert.Greater(events, 0);
            Assert.AreNotEqual(basePosition, _scenarioObject.transform.GetChild(0).localPosition);
        }
    }
}
