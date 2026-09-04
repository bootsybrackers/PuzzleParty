// Seeds synthetic Events data so the D1 retention (and roughly, level-difficulty) charts have
// something to render before real traffic exists.
//
// SAFETY: run this against PuzzleParty-Stage (or whatever your dev/test database is called),
// never production. Every fake document uses a "fake-" prefixed UserId/DeviceId so it's easy
// to spot and to delete later - see the cleanup query at the bottom of this file.
//
// How to run:
//   - mongosh, connected to your cluster: `use("PuzzleParty-Stage")` then paste this whole
//     file in, or `mongosh "<connection string>" --eval "$(cat seed-fake-events.mongo.js)"`.
//   - MongoDB Compass's embedded shell tab, connected to the same DB.
//   - Atlas Data Explorer's built-in terminal for the cluster.

use("PuzzleParty-Stage"); // adjust if your DB name differs

const NUM_DAYS = 14;              // how many days of install cohorts to fabricate
const USERS_PER_DAY = { min: 3, max: 12 };
const D1_RETURN_RATE = 0.4;       // ~40% of each day's cohort returns the next day
const REPLAY_RATE = 0.5;          // chance a D1-returner also keeps playing on later days

function randInt(min, max) { return Math.floor(Math.random() * (max - min + 1)) + min; }
function fakeId() { return [...Array(3)].map(() => Math.random().toString(16).slice(2, 10)).join("-"); }

const now = new Date();
const todayUTC = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));

const events = [];

for (let dayOffset = NUM_DAYS; dayOffset >= 1; dayOffset--) {
  const installDay = new Date(todayUTC.getTime() - dayOffset * 24 * 60 * 60 * 1000);
  const cohortSize = randInt(USERS_PER_DAY.min, USERS_PER_DAY.max);

  for (let i = 0; i < cohortSize; i++) {
    const userId = `fake-${installDay.toISOString().slice(0, 10)}-${i}-${fakeId()}`;
    const deviceId = `fake-device-${fakeId()}`;

    const installTime = new Date(installDay.getTime() + randInt(0, 23 * 60 + 59) * 60 * 1000);
    events.push({
      DeviceId: deviceId,
      UserId: userId,
      EventType: "app_install",
      Data: {},
      ClientTimestamp: installTime,
      ServerTimestamp: installTime
    });

    // Did they come back exactly one day later? Only possible if that day has happened yet.
    const d1Day = new Date(installDay.getTime() + 24 * 60 * 60 * 1000);
    if (d1Day <= now && Math.random() < D1_RETURN_RATE) {
      let day = d1Day;
      let keepPlaying = true;
      while (keepPlaying && day <= now) {
        const playTime = new Date(day.getTime() + randInt(0, 23 * 60 + 59) * 60 * 1000);
        events.push({
          DeviceId: deviceId,
          UserId: userId,
          EventType: "game_start",
          Data: { level: String(randInt(1, 12)) },
          ClientTimestamp: playTime,
          ServerTimestamp: playTime
        });

        // Also fake a level result so the level-difficulty chart has something too.
        events.push({
          DeviceId: deviceId,
          UserId: userId,
          EventType: "game_end",
          Data: { level: String(randInt(1, 12)), end_reason: Math.random() < 0.7 ? "success" : "fail" },
          ClientTimestamp: playTime,
          ServerTimestamp: playTime
        });

        day = new Date(day.getTime() + 24 * 60 * 60 * 1000);
        keepPlaying = Math.random() < REPLAY_RATE;
      }
    }
  }
}

const result = db.Events.insertMany(events);
print(`Inserted ${events.length} fake events across ${NUM_DAYS} cohort days.`);

// ---------------------------------------------------------------------------
// Cleanup - run this once you're done testing, to remove every fake document:
//
//   db.Events.deleteMany({ UserId: /^fake-/ });
// ---------------------------------------------------------------------------
