// Level difficulty pipeline
// -------------------------
// Run against the `Events` collection (PuzzleParty-Stage / PuzzleParty-Prod).
//
// Definition used here: for every level, out of all `game_start` attempts, what fraction of
// the matching `game_end` events were a success vs a fail. A level with a high fail rate (or
// low completion rate) relative to its neighbors is a good candidate for "too hard" - a spike
// in this chart is the signal to look at that level's move count / hole layout.
//
// Note: attempts/completions/fails are counted at the EVENT level, not per-user - a player who
// retries a level five times before beating it contributes multiple attempts and (usually) one
// completion. That's intentional: it's exactly the "how much friction did this level cause"
// signal we want, not a per-user pass/fail count.
//
// Output: one row per level -> { level, attempts, completions, fails, completionRatePct, failRatePct }.
//
// Where to run this:
//   - mongosh / MongoDB Compass, pointed at the PuzzleParty database, collection `Events`.
//   - MongoDB Atlas Charts: paste the array below (square brackets included) into the query
//     bar - typing `[...]` there is what tells Charts "this is an aggregation pipeline".

db.Events.aggregate([
  // 1. Only need the two signal event types for this metric.
  { $match: { EventType: { $in: ["game_start", "game_end"] } } },

  // 2. Data.level is stored as a string (matches the client's Dictionary<string,string>).
  //    Convert to a real number so the level axis sorts/plots numerically instead of as text
  //    ("10" before "2"). onError/onNull -> null lets us cleanly drop anything malformed next.
  { $addFields: {
      level: { $convert: { input: "$Data.level", to: "int", onError: null, onNull: null } }
  }},
  { $match: { level: { $ne: null } } },

  // 3. Count attempts (game_start) vs completions/fails (game_end split by end_reason).
  { $group: {
      _id: "$level",
      attempts: { $sum: { $cond: [ { $eq: ["$EventType", "game_start"] }, 1, 0 ] } },
      completions: {
        $sum: {
          $cond: [
            { $and: [ { $eq: ["$EventType", "game_end"] }, { $eq: ["$Data.end_reason", "success"] } ] },
            1, 0
          ]
        }
      },
      fails: {
        $sum: {
          $cond: [
            { $and: [ { $eq: ["$EventType", "game_end"] }, { $eq: ["$Data.end_reason", "fail"] } ] },
            1, 0
          ]
        }
      }
  }},

  // 4. Drop levels with no attempts at all (shouldn't happen, but keeps the rate math safe).
  { $match: { attempts: { $gt: 0 } } },

  { $addFields: {
      completionRatePct: { $round: [ { $multiply: [ { $divide: ["$completions", "$attempts"] }, 100 ] }, 1 ] },
      failRatePct: { $round: [ { $multiply: [ { $divide: ["$fails", "$attempts"] }, 100 ] }, 1 ] }
  }},

  { $sort: { _id: 1 } },

  { $project: {
      _id: 0,
      level: "$_id",
      attempts: 1,
      completions: 1,
      fails: 1,
      completionRatePct: 1,
      failRatePct: 1
  }}
])
