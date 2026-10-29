# sessionIndic

cTrader indicator that draws the Asia, London and New York sessions with their high/low and range in pips.

- hours are set in UTC (winter time)
- London and New York are shifted by 1h automatically in summer (EU and US rules)
- the high/low of the last finished sessions are extended until price closes through them

## Install

In cTrader: Algo > Indicators > New, name it `SessionIndic`, paste `SessionIndic/SessionIndic.cs` and build.
