# Conditioning summary

Files: 203 (loaded 203, failed 0).

192 files are different; 11 are byte-identical copies of an earlier file. Every file is processed, so the totals below count the copies too.

Entries in conditioning-report.csv: 4011. `note` = routine (3879), `warn` = worth a look (132, in 59 files). Filter the `level` column on `warn` to skip the routine rows.

## Changes made

| Change | Level | Files | Values or rows |
|---|---|---|---|
| Values equal to the declared NULL set to NaN | note | 179 | 5248733 |
| Values equal to a known marker (not declared) set to NaN | warn | 29 | 84661 |
| Depth reversed to ascending | note | 27 | |
| Depth sorted (jumps) | warn | 1 | 518 rows moved |
| Duplicate depths dropped (first kept) | warn | 1 | 200 rows |
| Depth converted from metres to feet | note | 0 | |
| Chosen curve converted to the standard unit | note | | 183048 rows |

Values set to NaN by the declared NULL or a marker: 5.9 % of all 91019017 curve values (5.8 % by the declared NULL, which is routine: a tool does not measure the whole well).

## Sampling (reported, never resampled)

- Header STEP differs from the real step: 2 files
- Header has no STEP: 0 files
- Files with gaps larger than 1.5 x the step: 0

## Curve choice

| Role | Files with a curve | Files without | Chosen curves (files) |
|---|---|---|---|
| GammaRay | 178 | 25 | GR 99, GRGC 29, GRSG 14, GR_EDTC 12, GAMMA 10, SGR 5, GRGM 3, HSGR 3 |
| Resistivity | 96 | 107 | RT_HRLT 40, DDLL 30, LLD 7, RD 6, RILD 6, RLLD 2, RT 2, AT90 1 |
| BulkDensity | 90 | 113 | RHOZ 45, DEN 26, RHOB 19 |
| NeutronPorosity | 74 | 129 | NPHI 55, TNPH 11, NPOR 5, CNLS 3 |

## Entries by category (files that have at least one)

- curve-choice: 203
- depth: 30
- duplicate-file: 11
- format: 11
- missing-value: 186
- parse: 17
- unit-conversion: 9

## Not loaded

None.
