## After (projection + covering index on `Quotes.AuthorId`)

```pwsh
PS C:\Users\aryan\repos\thinkschool-AryanBhalerao\Day11\Piece2> k6 run k6\load_test.js

         /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/


     execution: local
        script: k6\load_test.js
        output: -

     scenarios: (100.00%) 1 scenario, 10 max VUs, 1m20s max duration (incl. graceful stop):
              * default: Up to 10 looping VUs for 50s over 3 stages (gracefulRampDown: 30s, gracefulStop: 30s)


  █ THRESHOLDS

    http_req_duration
    ✓ 'p(50)<10000' p(50)=150.49ms
    ✓ 'p(99)<20000' p(99)=304.51ms

    http_req_failed
    ✓ 'rate<0.01' rate=0.00%


  █ TOTAL RESULTS

    checks_total.......: 2769    55.354034/s
    checks_succeeded...: 100.00% 2769 out of 2769
    checks_failed......: 0.00%   0 out of 2769

    ✓ status 200

    HTTP
    http_req_duration..............: avg=146.37ms min=18.1ms med=150.49ms max=467.99ms p(90)=219.62ms p(95)=242.33ms
      { expected_response:true }...: avg=146.37ms min=18.1ms med=150.49ms max=467.99ms p(90)=219.62ms p(95)=242.33ms
    http_req_failed................: 0.00% 0 out of 2769
    http_reqs......................: 2769   55.354034/s

    EXECUTION
    iteration_duration.............: avg=146.4ms  min=18.1ms med=150.49ms max=467.99ms p(90)=219.62ms p(95)=242.58ms
    iterations.....................: 2769   55.354034/s
    vus............................: 1      min=1         max=10
    vus_max........................: 10     min=10        max=10

    NETWORK
    data_received..................: 187 MB 3.7 MB/s
    data_sent......................: 235 kB 4.7 kB/s




running (0m50.0s), 00/10 VUs, 2769 complete and 0 interrupted iterations
default ✓ [ 100% ] 00/10 VUs  50s
```
