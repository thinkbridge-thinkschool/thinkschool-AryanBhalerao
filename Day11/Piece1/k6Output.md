```pwsh
PS C:\Users\aryan\repos\thinkschool-AryanBhalerao\Day11\Piece1> k6 run k6\load_test.js

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
    ✓ 'p(50)<10000' p(50)=2.22s
    ✓ 'p(99)<20000' p(99)=3.3s

    http_req_failed
    ✓ 'rate<0.01' rate=0.00%


  █ TOTAL RESULTS

    checks_total.......: 180     3.589674/s
    checks_succeeded...: 100.00% 180 out of 180
    checks_failed......: 0.00%   0 out of 180

    ✓ status 200

    HTTP
    http_req_duration..............: avg=2.28s min=328.19ms med=2.22s max=3.31s p(90)=3.24s p(95)=3.28s
      { expected_response:true }...: avg=2.28s min=328.19ms med=2.22s max=3.31s p(90)=3.24s p(95)=3.28s
    http_req_failed................: 0.00% 0 out of 180
    http_reqs......................: 180   3.589674/s

    EXECUTION
    iteration_duration.............: avg=2.28s min=328.19ms med=2.22s max=3.31s p(90)=3.24s p(95)=3.28s
    iterations.....................: 180   3.589674/s
    vus............................: 1     min=1        max=10
    vus_max........................: 10    min=10       max=10

    NETWORK
    data_received..................: 13 MB 266 kB/s
    data_sent......................: 17 kB 334 B/s




running (0m50.1s), 00/10 VUs, 180 complete and 0 interrupted iterations
default ✓ [======================================] 00/10 VUs  50s
```