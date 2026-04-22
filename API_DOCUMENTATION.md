# Nexus Arena API

Doc para o front.

Base:
- Dev local: `https://localhost:<porta>`
- Swagger: `/swagger`

Auth:
- JWT Bearer
- Header:

```http
Authorization: Bearer <token>
```

Regra:
- Se rota pedir auth e token faltar/inválido: `401`
- Se token existir mas sem permissão: `403`

## 1. Auth

### POST `/auth/register`

Cria conta.

Body:

```json
{
  "nickname": "Luiz",
  "email": "luiz@email.com",
  "password": "12345678",
  "role": 1
}
```

Enums:
- `role`
- `1 = Player`
- `2 = Organizer`
- `3 = Admin`

Sucesso `200`:

```json
{
  "token": "jwt",
  "user": {
    "id": "guid",
    "nickname": "Luiz",
    "email": "luiz@email.com",
    "role": 1,
    "totalXp": 0,
    "level": "Ferro",
    "totalWins": 0,
    "totalLosses": 0,
    "totalTitles": 0,
    "tournamentsParticipated": 0,
    "positiveRatingsReceived": 0,
    "consecutiveTitles": 0,
    "createdAtUtc": "2026-04-22T00:00:00Z"
  }
}
```

Erro `400`:

```json
{
  "errors": {
    "identity": [
      "Email ou nickname ja estao em uso."
    ]
  }
}
```

### POST `/auth/login`

Login.

Body:

```json
{
  "email": "luiz@email.com",
  "password": "12345678"
}
```

Sucesso `200`:
- mesmo shape do register

Erro:
- `401` credencial inválida

## 2. Catálogo de jogos

## GET `/games`

Lista jogos ativos.

Auth:
- não precisa

Sucesso `200`:

```json
[
  {
    "id": "guid",
    "title": "Valorant",
    "slug": "valorant",
    "isActive": true,
    "createdAtUtc": "2026-04-22T00:00:00Z"
  }
]
```

Uso no front:
- preencher select de criação de torneio
- `title` pode ser mostrado
- `id` vai em `gameCatalogItemId`

## POST `/games`

Cria item no catálogo.

Auth:
- precisa token
- só `Organizer` ou `Admin`

Body:

```json
{
  "title": "Counter-Strike 2"
}
```

Sucesso `201`:

```json
{
  "id": "guid",
  "title": "Counter-Strike 2",
  "slug": "counter-strike-2",
  "isActive": true,
  "createdAtUtc": "2026-04-22T00:00:00Z"
}
```

## 3. Torneios

Enums:

- `TournamentFormat`
- `1 = SingleElimination`
- `2 = GroupStage`

- `TournamentVisibility`
- `1 = Public`
- `2 = Private`

- `TournamentStatus`
- `1 = Draft`
- `2 = RegistrationOpen`
- `3 = RegistrationClosed`
- `4 = CheckInOpen`
- `5 = InProgress`
- `6 = Completed`
- `7 = Cancelled`
- `8 = CheckInClosed`

- `RegistrationStatus`
- `1 = Registered`
- `2 = Waitlisted`
- `3 = CheckedIn`
- `4 = Eliminated`
- `5 = Champion`
- `6 = Withdrawn`

### Shape base de torneio

```json
{
  "id": "guid",
  "name": "Nexus Cup",
  "gameTitle": "Valorant",
  "gameCatalogItemId": "guid",
  "format": 1,
  "visibility": 1,
  "status": 2,
  "maxParticipants": 16,
  "registeredCount": 12,
  "waitlistCount": 2,
  "startDateUtc": "2026-05-01T20:00:00Z",
  "registrationClosesAtUtc": "2026-04-30T20:00:00Z",
  "checkInOpensAtUtc": "2026-05-01T18:00:00Z",
  "checkInClosesAtUtc": "2026-05-01T19:30:00Z",
  "rules": "texto",
  "organizerId": "guid",
  "createdAtUtc": "2026-04-22T00:00:00Z",
  "completedAtUtc": null
}
```

### GET `/tournaments`

Lista torneios.

Query params:
- `game` opcional. Filtra por `GameTitle`
- `status` opcional. Filtra por enum numérico

Auth:
- opcional

Regra:
- torneio público: aparece para todos
- torneio privado: só aparece para organizador e inscritos

Sucesso `200`:

```json
[
  {
    "id": "guid",
    "name": "Nexus Cup",
    "gameTitle": "Valorant",
    "gameCatalogItemId": "guid",
    "format": 1,
    "visibility": 1,
    "status": 2,
    "maxParticipants": 16,
    "registeredCount": 12,
    "waitlistCount": 2,
    "startDateUtc": "2026-05-01T20:00:00Z",
    "registrationClosesAtUtc": "2026-04-30T20:00:00Z",
    "checkInOpensAtUtc": "2026-05-01T18:00:00Z",
    "checkInClosesAtUtc": "2026-05-01T19:30:00Z",
    "rules": "texto",
    "organizerId": "guid",
    "createdAtUtc": "2026-04-22T00:00:00Z",
    "completedAtUtc": null
  }
]
```

### GET `/tournaments/{tournamentId}`

Detalhe completo.

Auth:
- opcional
- se privado, precisa ser organizador ou inscrito

Sucesso `200`:

```json
{
  "tournament": {
    "id": "guid",
    "name": "Nexus Cup",
    "gameTitle": "Valorant",
    "gameCatalogItemId": "guid",
    "format": 2,
    "visibility": 1,
    "status": 5,
    "maxParticipants": 16,
    "registeredCount": 16,
    "waitlistCount": 0,
    "startDateUtc": "2026-05-01T20:00:00Z",
    "registrationClosesAtUtc": "2026-04-30T20:00:00Z",
    "checkInOpensAtUtc": "2026-05-01T18:00:00Z",
    "checkInClosesAtUtc": "2026-05-01T19:30:00Z",
    "rules": "texto",
    "organizerId": "guid",
    "createdAtUtc": "2026-04-22T00:00:00Z",
    "completedAtUtc": null
  },
  "registrations": [
    {
      "id": "guid",
      "tournamentId": "guid",
      "userId": "guid",
      "status": 3,
      "registeredAtUtc": "2026-04-22T00:00:00Z",
      "checkedInAtUtc": "2026-04-22T00:10:00Z",
      "seed": 1,
      "wins": 2,
      "losses": 1,
      "finalPlacement": null
    }
  ],
  "groups": [
    {
      "id": "guid",
      "name": "Grupo A",
      "order": 1,
      "standings": [
        {
          "registrationId": "guid",
          "userId": "guid",
          "nickname": "Luiz",
          "points": 6,
          "wins": 2,
          "losses": 0,
          "scoreFor": 4,
          "scoreAgainst": 1
        }
      ]
    }
  ],
  "matches": [
    {
      "id": "guid",
      "tournamentId": "guid",
      "tournamentGroupId": "guid",
      "stage": 2,
      "status": 3,
      "roundNumber": 1,
      "sequence": 1,
      "playerOneRegistrationId": "guid",
      "playerTwoRegistrationId": "guid",
      "playerOneScore": 2,
      "playerTwoScore": 1,
      "winnerRegistrationId": "guid",
      "nextMatchId": null,
      "nextMatchSlot": null,
      "resultReportedAtUtc": "2026-04-22T00:00:00Z",
      "playerOneConfirmedAtUtc": "2026-04-22T00:01:00Z",
      "playerTwoConfirmedAtUtc": "2026-04-22T00:02:00Z",
      "confirmedAtUtc": "2026-04-22T00:02:00Z",
      "disputedAtUtc": null,
      "resolutionNote": null
    }
  ]
}
```

### POST `/tournaments`

Cria torneio.

Auth:
- precisa token
- só `Organizer` ou `Admin`

Body:

```json
{
  "name": "Nexus Cup",
  "gameTitle": "Valorant",
  "gameCatalogItemId": "guid",
  "format": 1,
  "visibility": 1,
  "maxParticipants": 16,
  "startDateUtc": "2026-05-01T20:00:00Z",
  "registrationClosesAtUtc": "2026-04-30T20:00:00Z",
  "checkInOpensAtUtc": "2026-05-01T18:00:00Z",
  "checkInClosesAtUtc": "2026-05-01T19:30:00Z",
  "rules": "texto do regulamento"
}
```

Sucesso `201`:
- retorna shape base de torneio

### PUT `/tournaments/{tournamentId}`

Edita torneio.

Auth:
- precisa token
- organizador dono ou admin

Regra:
- não edita se `InProgress`, `Completed`, `Cancelled`

Body:
- mesmo shape do create

Sucesso `200`

### DELETE `/tournaments/{tournamentId}`

Remove torneio.

Auth:
- precisa token
- organizador dono ou admin

Regra:
- não remove se `InProgress`

Sucesso:
- `204`

### POST `/tournaments/{tournamentId}/register`

Inscreve jogador.

Auth:
- precisa token

Regra:
- se lotado, vira `Waitlisted`
- se aberto, vira `Registered`

Sucesso `200`:

```json
{
  "id": "guid",
  "tournamentId": "guid",
  "userId": "guid",
  "status": 1,
  "registeredAtUtc": "2026-04-22T00:00:00Z",
  "checkedInAtUtc": null,
  "seed": null,
  "wins": 0,
  "losses": 0,
  "finalPlacement": null
}
```

### POST `/tournaments/{tournamentId}/withdraw`

Jogador desiste.

Auth:
- precisa token

Regra:
- seta `Withdrawn`
- tenta puxar alguém da waitlist

Sucesso `200`:
- retorna registration

### POST `/tournaments/{tournamentId}/close-registration`

Fecha inscrição.

Auth:
- organizador dono ou admin

Sucesso `200`:

```json
{
  "id": "guid",
  "status": 3
}
```

### POST `/tournaments/{tournamentId}/open-check-in`

Abre check-in.

Auth:
- organizador dono ou admin

Sucesso `200`

### POST `/tournaments/{tournamentId}/check-in`

Jogador faz check-in.

Auth:
- precisa token

Regra:
- só para `Registered`
- torneio deve estar `CheckInOpen`
- vira `CheckedIn`

### POST `/tournaments/{tournamentId}/close-check-in`

Fecha check-in.

Auth:
- organizador dono ou admin

Regra:
- quem ficou `Registered` sem check-in vira `Withdrawn`
- tenta puxar waitlist
- status do torneio vira `CheckInClosed`

### POST `/tournaments/{tournamentId}/start`

Inicia torneio.

Auth:
- organizador dono ou admin

Regra:
- gera bracket para `SingleElimination`
- gera grupos e partidas round-robin para `GroupStage`
- status vira `InProgress`

Sucesso `200`:

```json
{
  "id": "guid",
  "status": 5
}
```

## 4. Partidas

Enums:
- `MatchStage`
- `1 = Bracket`
- `2 = Group`

- `MatchStatus`
- `1 = Pending`
- `2 = PendingConfirmation`
- `3 = Confirmed`
- `4 = Disputed`

- `DisputeStatus`
- `1 = Open`
- `2 = ResolvedAccepted`
- `3 = ResolvedRejected`

### POST `/matches/{matchId}/report-result`

Organizador lança placar.

Auth:
- organizador do torneio ou admin

Body:

```json
{
  "playerOneScore": 2,
  "playerTwoScore": 1
}
```

Regra:
- empate não aceito
- vira `PendingConfirmation`

Sucesso `200`:
- retorna shape de match

### POST `/matches/{matchId}/confirm`

Jogador confirma resultado.

Auth:
- um dos dois jogadores da partida

Regra:
- quando os dois confirmam:
  - match vira `Confirmed`
  - bracket avança
  - XP atualiza
  - grupos atualizam
  - se grupos acabaram, gera mata-mata
  - se torneio acabou, fecha torneio

### POST `/matches/{matchId}/reject`

Jogador contesta resultado.

Auth:
- um dos dois jogadores da partida

Body:

```json
{
  "reason": "Placar foi registrado errado."
}
```

Regra:
- match vira `Disputed`
- cria disputa
- notifica organizador

### POST `/matches/{matchId}/resolve-dispute`

Organizador resolve disputa.

Auth:
- organizador do torneio ou admin

Body:

```json
{
  "acceptResult": true,
  "resolutionNote": "Print conferido."
}
```

Regra:
- `acceptResult = true`
  - disputa fecha como aceita
  - match vira `Confirmed`
  - aplica avanço e XP
- `acceptResult = false`
  - disputa fecha como rejeitada
  - match volta para `PendingConfirmation`
  - confirmações limpam

## 5. Feedback

### POST `/feedback/tournaments/{tournamentId}`

Avalia outro jogador.

Auth:
- precisa token

Body:

```json
{
  "toUserId": "guid",
  "rating": 5,
  "comment": "Jogou limpo."
}
```

Regra:
- não pode avaliar a si mesmo
- os dois precisam ter participado do torneio
- nota `>= 4` soma `PositiveRatingsReceived`
- nota `>= 4` dá XP positivo

Sucesso `200`:

```json
{
  "id": "guid",
  "rating": 5,
  "createdAtUtc": "2026-04-22T00:00:00Z"
}
```

## 6. Perfil

### GET `/profile/me`

Perfil do usuário logado.

Auth:
- precisa token

### GET `/players/{nickname}`

Perfil público.

Auth:
- não precisa

Shape `200`:

```json
{
  "user": {
    "id": "guid",
    "nickname": "Luiz",
    "email": "luiz@email.com",
    "role": 1,
    "totalXp": 240,
    "level": "Bronze",
    "totalWins": 7,
    "totalLosses": 3,
    "totalTitles": 1,
    "tournamentsParticipated": 4,
    "positiveRatingsReceived": 2,
    "consecutiveTitles": 1,
    "createdAtUtc": "2026-04-22T00:00:00Z"
  },
  "achievements": [
    {
      "code": 1,
      "name": "Estreante",
      "description": "Participe do seu primeiro torneio",
      "unlockedAtUtc": "2026-04-22T00:00:00Z"
    }
  ],
  "history": [
    {
      "tournamentId": "guid",
      "tournamentName": "Nexus Cup",
      "gameTitle": "Valorant",
      "format": 1,
      "tournamentStatus": 6,
      "registrationStatus": 5,
      "wins": 3,
      "losses": 0,
      "finalPlacement": 1,
      "registeredAtUtc": "2026-04-22T00:00:00Z"
    }
  ]
}
```

Nota:
- hoje email sai junto no response. Se o front quiser perfil público sem email, isso precisa mudar no back.

## 7. Notificações

Enums:
- `NotificationType`
- `1 = WaitlistPromotion`
- `2 = TournamentUpdate`
- `3 = MatchDispute`
- `4 = General`

### GET `/notifications`

Lista notificações do usuário logado.

Auth:
- precisa token

Sucesso `200`:

```json
[
  {
    "id": "guid",
    "tournamentId": "guid",
    "type": 1,
    "message": "Uma vaga foi liberada no torneio Nexus Cup. Sua inscricao foi confirmada.",
    "isRead": false,
    "createdAtUtc": "2026-04-22T00:00:00Z"
  }
]
```

### POST `/notifications/{notificationId}/read`

Marca como lida.

Auth:
- precisa token

Sucesso `200`:

```json
{
  "id": "guid",
  "isRead": true
}
```

## 8. Ranking

### GET `/ranking/global`

Query params:
- `game` opcional
- `page` opcional
- `pageSize` opcional

Auth:
- não precisa

Sucesso `200`:

```json
[
  {
    "position": 1,
    "nickname": "Luiz",
    "level": "Diamante",
    "xp": 8200,
    "titles": 4
  }
]
```

## 9. Regras de XP

Hoje back usa:
- inscrever-se em torneio: `+10`
- vencer partida de grupos: `+25`
- vencer partida eliminatória: `+40`
- conquistar campeonato: `+150`
- participar sem desistir: `+15`
- avaliação positiva: `+5`

Níveis:
- `0-499 = Ferro`
- `500-1499 = Bronze`
- `1500-2999 = Prata`
- `3000-5999 = Ouro`
- `6000-9999 = Diamante`
- `10000+ = Nexus`

## 10. Regras de conquista

Back tenta liberar:
- `Estreante`
- `PrimeiraVitoria`
- `Veterano`
- `Invicto`
- `Lenda`
- `HatTrick`
- `FairPlay`
- `Dominador`

## 11. Fluxos do front

### Fluxo: criar torneio

1. Buscar `/games`
2. Usuário escolhe jogo
3. Enviar `POST /tournaments`
4. Guardar `id`
5. Redirecionar para tela do torneio

### Fluxo: entrar em torneio

1. Listar `/tournaments`
2. Ver detalhe `/tournaments/{id}`
3. Enviar `POST /tournaments/{id}/register`
4. Tratar `status`:
   - `1 Registered`: vaga ok
   - `2 Waitlisted`: entrou na fila

### Fluxo: check-in

1. Ver `tournament.status == 4`
2. Mostrar botão
3. Enviar `POST /tournaments/{id}/check-in`

### Fluxo: organizador inicia torneio

1. Opcional: `close-registration`
2. Opcional: `open-check-in`
3. Opcional: `close-check-in`
4. `POST /tournaments/{id}/start`
5. Recarregar detalhe

### Fluxo: placar

1. Organizador envia `report-result`
2. Jogador A confirma
3. Jogador B confirma
4. Front atualiza bracket

### Fluxo: disputa

1. Jogador rejeita com `reject`
2. Organizador vê notificação
3. Organizador resolve com `resolve-dispute`

## 12. Pontos de atenção para o front

- Muitos enums vêm como número. O front deve mapear para label.
- `gameTitle` ainda existe mesmo com catálogo. Use `gameCatalogItemId` para select e `gameTitle` para exibição.
- Em torneio privado, lista e detalhe dependem do usuário autenticado.
- `players/{nickname}` hoje expõe email. Se isso for problema, ajustar no back.
- `GroupStage` hoje gera grupos e depois mata-mata automático quando todas partidas de grupo acabam.
- Critério de classificação do grupo:
  - pontos
  - vitórias
  - saldo (`scoreFor - scoreAgainst`)
  - score a favor
- Waitlist usa ordem de inscrição.
- Sem WebSocket. Atualização é por refresh/polling.

## 13. Erros comuns

Shape comum de validação:

```json
{
  "errors": {
    "campo": [
      "mensagem"
    ]
  }
}
```

Status comuns:
- `200` ok
- `201` criado
- `204` sem conteúdo
- `400` validação
- `401` não autenticado
- `403` sem permissão
- `404` não encontrado
- `500` erro interno

## 14. Endpoints resumo

- `POST /auth/register`
- `POST /auth/login`
- `GET /games`
- `POST /games`
- `GET /tournaments`
- `GET /tournaments/{id}`
- `POST /tournaments`
- `PUT /tournaments/{id}`
- `DELETE /tournaments/{id}`
- `POST /tournaments/{id}/register`
- `POST /tournaments/{id}/withdraw`
- `POST /tournaments/{id}/close-registration`
- `POST /tournaments/{id}/open-check-in`
- `POST /tournaments/{id}/check-in`
- `POST /tournaments/{id}/close-check-in`
- `POST /tournaments/{id}/start`
- `POST /matches/{id}/report-result`
- `POST /matches/{id}/confirm`
- `POST /matches/{id}/reject`
- `POST /matches/{id}/resolve-dispute`
- `POST /feedback/tournaments/{id}`
- `GET /profile/me`
- `GET /players/{nickname}`
- `GET /notifications`
- `POST /notifications/{id}/read`
- `GET /ranking/global`

