DOOR MEET — 3회차 협동 MR 예제
Unity 6000.3.25f1 / 기존 MY_VR 안의 별도 실습

열 씬: Assets/Session3_DoorMeet/Scenes/DoorMeet.unity
상단 메뉴: Session 3 Door Meet > 1. Open or Create Scene

폴더 안내
Scenes: 이번 실습 씬
Scripts/Networking: 같은 Wi-Fi의 2명 연결 및 미션 상태 동기화
Scripts/Gameplay: 잡을 열쇠, XR 받침대, 선택 실행형 PC 검증
Scripts/Spatial: Quest 공간 읽기, 문 정렬, 패스스루 공개
Scripts/UI: 한글 연결·미션·공간 확인 메뉴
Materials/Shaders: 가상 공간 재질과 문 공개용 셰이더
Prefabs: 파란 열쇠와 빨간 열쇠
Settings: 이번 씬 실행 중에만 사용하는 URP 설정
Fonts: Noto Sans CJK KR와 OFL 라이선스
Documentation: 사용 안내와 검증 결과

어떤 경험인가?
실제 문 양쪽의 인접한 공간에서 두 사람이 각자의 미션을 수행한다.
A는 세 자리 암호를 입력한다. B는 맞는 색의 열쇠를 잡아서 받침대에 놓는다.
각자가 가진 힌트는 상대의 문제를 푸는 데 필요하다. 같은 현장에서 말로 전달한다.
두 미션 모두 성공하면 문 위치에 맞춘 공개 영역에서 실제 문이 보인다.
실제 문은 사람이 직접 연다. 문을 열면 상대의 실제 모습이 패스스루로 보인다.
문 가까이에서는 현실 전체 보기로 전환한다.

구현된 기본 규칙
한 Quest가 방장(Host)으로 동작하며 별도 인터넷 서버 계정은 필요하지 않다.
다른 Quest는 방장의 IPv4 주소를 입력해 참가한다. UDP 포트는 7779다.
동시에 두 사람만 허용한다. 세 번째 참가자는 거절한다.
준비 상태, 각자 미션 결과, 문 공개 상태, 재시작 횟수만 동기화한다.
각 기기의 방·문·책상 위치는 그 기기의 MRUK 공간 데이터로 맞춘다.
방장만 다시 시작할 수 있다. 연결이 끊기면 문 공개와 진행 상태를 해제한다.
정렬을 바꾸면 자신의 준비를 해제하고 다시 확인하게 한다.

PC에서 직접 따라 하기
1. Builds/Session3_DoorMeet_PC/StartTwoPlayers.cmd를 더블클릭해 PC 창 두 개를 연다.
   또는 DoorMeet.exe를 두 번 열고 Alt+Enter로 창 모드로 전환한다.
2. 첫 창의 ① 연결에서 'A · 방 만들기'를 누른다.
3. 두 번째 창에서 'PC 테스트 주소: 127.0.0.1'을 누르고 'B · 참가하기'를 누른다.
4. 두 창 모두 ③ 공간 맞춤으로 이동해 '문·책상 위치 확인 완료'를 누른다.
5. 두 창 모두 ② 미션에서 준비 버튼을 누른다.
6. B의 힌트에서 A 암호를 읽고 A 창에 3, 1, 4를 입력해 제출한다.
7. A의 힌트에서 B 열쇠 색을 읽는다. B 창에서 파란 열쇠를 선택하고 받침대에 놓는다.
8. 두 창 모두 성공 상태가 된다. 모의 문의 패널이 사라지고 상대 표시가 보인다.
9. A의 '처음부터 다시'를 누르면 두 창의 준비·성공·문 상태가 초기화된다.
PC의 방과 상대 표시는 모의 화면이다. 실제 패스스루·문 정렬을 검증하는 화면이 아니다.

Quest에서 이어서 확인할 절차 (기기 연결 후 별도 APK 빌드 승인 필요)
권장 기기: Quest 3 또는 Quest 3S 두 대. Quest 2는 흑백 패스스루와 공간 설정 제약을
실기에서 확인해야 하며, 이 프로젝트의 기기 검증 대상으로 아직 확정하지 않았다.
1. 두 Quest의 시스템 공간 설정에서 각 방의 벽·바닥·책상·실제 문틀을 등록한다.
2. Unity 상단의 Session 3 Door Meet > 2. Configure Quest Features (no build)를 실행하면
   Android의 Meta XR OpenXR 기능, 공간 사용, 패스스루 사용 설정을 적용한다.
   공용 Android 설정에 적용되므로 다른 실습 APK의 기능 요구 조건도 함께 검토한다.
3. XR Plug-in Management의 Android OpenXR, Oculus Touch 컨트롤러 프로파일,
   Meta XR 기능을 확인한다. Meta의 Project Setup Tool 권장 설정을 검토한다.
4. 승인 후 Android 빌드 프로필에 DoorMeet 씬만 넣어 APK를 만들고 두 기기에 설치한다.
5. 공간 사용 권한을 허용한다. 처음에는 현실 전체 보기 상태로 시작한다.
6. ③ 공간 맞춤에서 문틀을 선택하고 실제 문·손잡이와 흰 프레임이 맞는지 확인한다.
   폭·높이·방향을 조정할 수 있다. 문틀이 없다면 시스템 공간 설정을 수정하고 다시 읽는다.
7. 책상 정보가 있으면 실제 책상 위치·크기로 생성한 가상 공간 위에 미션 소품을 배치한다.
   책상 정보가 없으면 모의 받침대가 남으므로 실제 위치에 맞춘 실습을 위해 책상을 등록한다.
8. 위치 확인을 마치고 같은 Wi-Fi에서 A가 방을 만들고 B가 표시된 IPv4 주소로 참가한다.
   휴대폰 핫스폿·게스트 Wi-Fi의 기기 간 통신 차단은 연결을 방해할 수 있다.
9. B는 준비 후 메뉴가 숨겨지면 컨트롤러 그립으로 파란 열쇠를 잡아 흰 받침대에 놓는다.
   왼쪽 컨트롤러 X 버튼으로 메뉴를 다시 보거나 숨긴다. A는 레이로 암호 버튼을 누른다.
10. 둘 다 성공하면 문 영역이 공개된다. 실제 문을 직접 열어 상대를 확인한다.
    문 주변 1.2m 안에서는 전체 현실 보기로 전환하며, 멀어져 1.7m를 넘으면 문 공개로 돌아온다.
    언제든 메뉴의 현실 전체 보기 버튼 또는 오른쪽 컨트롤러 B 버튼을 사용할 수 있다.

문제 해결
연결 실패: 같은 Wi-Fi인지, 방장 IPv4인지, UDP 7779 통신 허용인지 확인한다.
PC의 127.0.0.1은 같은 PC에서만 쓴다. 다른 Quest에는 방장 Quest의 LAN 주소를 쓴다.
방이 가득 찼다는 표시: 기존 B의 연결을 종료한 뒤 다시 참가한다.
미션이 시작되지 않음: 두 사람 연결, 문 위치 확인, 양쪽 준비 완료를 확인한다.
틀린 열쇠가 받침대에 붙음: 그립으로 집어서 빼고 맞는 열쇠로 바꾼다.
정답이 안 됨: A는 314, B는 파란 열쇠다. 상대의 힌트를 먼저 읽는다.
문틀 없음: Quest 시스템 공간 설정에서 실제 문틀을 추가하고 공간 다시 읽기를 누른다.
문 위치가 다름: 다른 문틀을 선택한다. 정렬을 확인하기 전에는 준비하지 않는다.
패스스루가 안 보임: 공간·패스스루 권한과 Meta XR 기능을 확인한다. PC에서는 보이지 않는다.
Quest에서 실제 벽·책상 경계가 어긋남: 시스템 공간 설정을 다시 수행하고 현실 전체 보기에서 검토한다.
어두운 공간: 추적·공간 읽기·패스스루 화면은 조명 상태의 영향을 받는다. 조명을 켜고 실기 검증한다.

범위와 검증 구분
현재 구현은 LAN 두 사람과 로컬 공간 정렬을 이용한다. 공유 공간 앵커·인터넷 매칭·음성 채팅·
자동 실제 문 각도 감지·사람 인식·이동하는 물체 추적은 포함하지 않는다.
상대방은 실제 현장의 패스스루로 보이며, 네트워크 아바타를 보내는 방식이 아니다.
PC 검증과 기기 검증은 별개다. 현재 Quest가 연결되어 있지 않아 Android 실행, 공간 권한,
문 영역 셰이더, 실제 정렬, 현장 동선의 적합성은 Quest 두 대로 추가 확인해야 한다.
기존 SampleScene, BasicScene, StageDecorating의 원본 씬은 이 예제를 만드는 과정에서 수정하지 않는다.

공식 참고 자료
Unity Netcode: https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@2.7/manual/index.html
Meta MRUK: https://developers.meta.com/vr/documentation/unity/unity-mr-utility-kit-overview/
Meta Virtual Home: https://developers.meta.com/vr/documentation/unity/unity-sample-mruk-virtual-home/
Meta Passthrough Windows: https://developers.meta.com/vr/documentation/unity/unity-customize-passthrough-passthrough-windows/

의존 패키지
com.unity.netcode.gameobjects 2.7.0
com.meta.xr.sdk.core 207.0.0
com.meta.xr.mrutilitykit 207.0.0
기존 XRI 3.5.1 / OpenXR 1.17.1 / URP 17.3.0 유지
Noto Sans CJK KR는 OFL 라이선스에 따라 배포하며 Fonts/OFL.txt를 함께 보존한다.
