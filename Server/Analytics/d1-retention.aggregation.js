// D1 retention pipeline
// ----------------------
// Run against the `Events` collection (PuzzleParty-Stage / PuzzleParty-Prod).
//
// Definition used here:
//   - Cohort day  = the UTC calendar day of a user's `app_install` event.
//   - "Returned"  = the user has a `game_start` event on cohort day + 1.
//     (Using game_start rather than "any event" on purpose - we want to know they came
//     back and actually played a level, not just that a background sync pinged the server.)
//
// Output: one row per cohort day -> { cohortDay, cohortSize, retainedD1, d1RetentionPct }.
// Cohorts that haven't reached their D1 checkpoint yet (installed today/yesterday, depending
// on time of day) are dropped rather than shown as a misleading 0%.
//
// Where to run this:
//   - mongosh / MongoDB Compass, pointed at the PuzzleParty database, collection `Events`.
//   - MongoDB Atlas Charts: create a chart, pick `Events` as the data source, and paste the
//     array below (square brackets included) into the query bar - typing `[...]` there is
//     what tells Charts "this is an aggregation pipeline" rather than a plain filter.
//
// Timestamp choice: uses ServerTimestamp (set by the API when the event lands), not
// ClientTimestamp (set on-device, queued, and possibly synced up to ~10 minutes or a full
// session later). ServerTimestamp avoids client clock-drift issues; the trade-off is a
// (usually small) risk of an event landing just after a day boundary if it was queued near
// midnight. Swap every $ServerTimestamp below for $ClientTimestamp if you'd rather bucket by
// when the user actually did the thing, drift and all.
//
// Note on $$NOW: an earlier version of this pipeline filtered immature cohorts with
// `d1Day <= $$NOW`. MongoDB Atlas Charts rejects `$$NOW` in a custom pipeline (confirmed by
// testing against the actual dashboard) - likely because it caches aggregation results and a
// wall-clock-dependent operator would make that caching incoherent. The $setWindowFields
// stage below computes `dataHorizon` - the latest ServerTimestamp actually present in the
// data - and uses that as the cutoff instead. This sidesteps the restriction and is arguably
// more correct anyway: it reflects the true data horizon rather than wall-clock time, which
// can run ahead of what's actually been synced from clients.

db.Events.aggregate([
  // 1. Only need the two signal event types for this metric.
  { $match: { EventType: { $in: ["app_install", "game_start"] } } },

  // 2. Stamp every matched document with the latest ServerTimestamp seen across all of them -
  //    our stand-in for "now" that doesn't rely on $$NOW.
  { $setWindowFields: {
      sortBy: { ServerTimestamp: 1 },
      output: {
        dataHorizon: { $max: "$ServerTimestamp", window: { documents: ["unbounded", "unbounded"] } }
      }
  }},

  // 3. Per user: the day they installed, the set of distinct days they had a game_start, and
  //    the (constant) data horizon carried through from stage 2.
  //    $$REMOVE makes the field "missing" for non-matching docs, which $min/$addToSet
  //    correctly ignore (passing an explicit null instead would make $min always return
  //    null, since null sorts lowest of all BSON types).
  { $group: {
      _id: "$UserId",
      installDay: {
        $min: {
          $cond: [
            { $eq: ["$EventType", "app_install"] },
            { $dateTrunc: { date: "$ServerTimestamp", unit: "day" } },
            "$$REMOVE"
          ]
        }
      },
      activeDays: {
        $addToSet: {
          $cond: [
            { $eq: ["$EventType", "game_start"] },
            { $dateTrunc: { date: "$ServerTimestamp", unit: "day" } },
            "$$REMOVE"
          ]
        }
      },
      dataHorizon: { $max: "$dataHorizon" }
  }},

  // 4. Drop users with no recorded install event (e.g. data from before app_install existed).
  { $match: { installDay: { $ne: null } } },

  // 5. Did they have a game_start exactly one calendar day after install?
  { $addFields: {
      d1Day: { $dateAdd: { startDate: "$installDay", unit: "day", amount: 1 } }
  }},
  { $addFields: {
      returnedD1: { $in: ["$d1Day", "$activeDays"] }
  }},

  // 6. Exclude cohorts too young to have reached their D1 checkpoint yet, using the data
  //    horizon from stage 2/3 instead of $$NOW. $expr is required here since we're comparing
  //    two fields on the same document rather than a field to a literal.
  { $match: { $expr: { $lte: ["$d1Day", "$dataHorizon"] } } },

  // 7. Roll individual users up into a per-cohort-day retention curve.
  { $group: {
      _id: "$installDay",
      cohortSize: { $sum: 1 },
      retainedD1: { $sum: { $cond: ["$returnedD1", 1, 0] } }
  }},

  { $addFields: {
      d1RetentionPct: {
        $round: [
          { $multiply: [ { $divide: ["$retainedD1", "$cohortSize"] }, 100 ] },
          1
        ]
      }
  }},

  { $sort: { _id: 1 } },

  { $project: {
      _id: 0,
      cohortDay: "$_id",
      cohortSize: 1,
      retainedD1: 1,
      d1RetentionPct: 1
  }}
])
