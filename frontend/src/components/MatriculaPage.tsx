import React, { useEffect, useState } from 'react'
import Paper from '@material-ui/core/Paper'
import TextField from '@material-ui/core/TextField'
import Autocomplete from '@material-ui/lab/Autocomplete'
import { Button, Table, TableBody, TableCell, TableHead, TableRow } from '@material-ui/core'
import IconButton from '@material-ui/core/IconButton'
import { EstudanteModel, Matricula } from '../model/estudante-model'
import AddIcon from '@material-ui/icons/Add'
import { makeStyles } from '@material-ui/core/styles'
import { addMatricula, getEstudanteById } from '../services/pessoa-service'
import { useSnackbar } from 'notistack'
import { getCursos } from '../services/cursos-service'
import { v4 as uuidv4 } from 'uuid'
import { useHistory, useParams } from 'react-router-dom'
import { getGradesByCursoId } from '../services/grades-service'
import { ativarMatricula } from '../services/matricula-service'

const useStyles = makeStyles(() => ({
  fields: {
    margin: '10px'
  },
  cancelButton: {
    color: 'white'
  }
}))

const MatriculaPage: React.FC = () => {
  const history = useHistory()
  const { estudanteId } = useParams()
  const [estudante, setEstudante] = useState<EstudanteModel>()
  const [cursos, setCursos] = useState([])
  const [selectedCurso, setSelectedCurso] = useState<any>({})
  const [grades, setGrades] = useState([])
  const [selectedGrade, setSelectedGrade] = useState<any>({})
  const [mode, setMode] = useState<'edit' | 'add' | 'none'>('none')
  const classes = useStyles()
  const { enqueueSnackbar } = useSnackbar()

  useEffect(() => {
    getEstudanteById(estudanteId).then((res: any) => {
      setEstudante(res.data)
    })

    getCursos().then((res: any) => {
      setCursos(res.data)
    })
  }, [estudanteId])

  useEffect(() => {
    if (selectedCurso?.id) {
      getGradesByCursoId(selectedCurso.id).then((res: any) => {
        setGrades(res.data)
      })
    } else {
      setGrades([])
    }
  }, [selectedCurso])

  const openAdd = () => {
    setMode('add')
  }

  const onAtivarMatricula = (matriculaId) => {
    ativarMatricula(matriculaId).then(() => {
      getEstudanteById(estudanteId).then((res: any) => {
        setEstudante(res.data)
        enqueueSnackbar('Matricula ativada com sucesso.', { variant: 'success' })
      })
    })
  }

  const table = estudante?.matriculas
    ? <Table>
      <TableHead>
        <TableRow>
          <TableCell>Nome</TableCell>
          <TableCell>Grade</TableCell>
          <TableCell>Situação</TableCell>
          <TableCell align="right">
            <IconButton style={{ backgroundColor: '#2196f3' }} onClick={openAdd} aria-label="add">
              <AddIcon style={{ color: 'white' }} fontSize="large"/>
            </IconButton>
          </TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {estudante.matriculas.map((matricula: Matricula) => (
          <TableRow key={matricula?.id}>
            <TableCell component="th" scope="row">
              {matricula?.grade?.curso?.nome}
            </TableCell>
            <TableCell component="th" scope="row">
              {matricula?.grade?.ano}
            </TableCell>
            <TableCell component="th" scope="row">
              {matricula?.ativo ? 'Ativada' : 'Desativada'}
            </TableCell>
            <TableCell align="right">
              <Button variant="contained" color="primary" onClick={() => onAtivarMatricula(matricula?.id)}>
                Ativar
              </Button>
              <Button style={{ marginLeft: 5 }} variant="contained" color="primary"
                onClick={() => history.push(`/matricula/${estudanteId}/inscricaotodos/${matricula.id}`)}>
                Histórico do Aluno
              </Button>
              <Button style={{ marginLeft: 5 }} variant="contained" color="primary"
                onClick={() => history.push(`/matricula/${estudanteId}/inscricaoatual/${matricula.id}`)}>
                Inscrições Desse Semestre
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
    : <div></div>

  const onSave = () => {
    const cmd = {
      pessoaId: estudante.id,
      gradeId: selectedGrade.id,
      matriculaId: uuidv4()
    }

    addMatricula(cmd).then(() => {
      getEstudanteById(estudanteId).then((res: any) => {
        setEstudante(res.data)
        enqueueSnackbar('Matricula criado com sucesso.', { variant: 'success' })
      })
    })
  }

  const onCancel = () => {
    setMode('none')
  }

  return (
    <Paper style={{ height: '80vh', marginLeft: '4vw' }}>

      <div style={{
        padding: '10px',
        textAlign: 'left',
        borderBottom: '1px solid rgb(224, 224, 224)',
        backgroundColor: '#2196F3',
        color: 'white'
      }}>Matriculas do {estudante?.nome}</div>

      {mode !== 'none'
        ? <div>
          <form style={{ display: 'flex', justifyContent: 'center' }} noValidate autoComplete="off">

            <Autocomplete
              value={selectedCurso}
              onChange={(event: any, newValue: any | null) => {
                setSelectedCurso(newValue)
              }}
              id="combo-box-demo"
              options={cursos}
              getOptionLabel={(option) => option.nome}
              style={{ width: 300, margin: 10 }}
              renderInput={(params) => <TextField {...params} label="Curso"/>}
            />

            <Autocomplete
              value={selectedGrade}
              onChange={(event: any, newValue: any | null) => {
                setSelectedGrade(newValue)
              }}
              id="combo-box-demo"
              options={grades}
              getOptionLabel={(option) => option?.ano ? '' + option?.ano : option?.ano}
              style={{ width: 300, margin: 10 }}
              renderInput={(params) => <TextField {...params} label="Grade"/>}
            />

          </form>
          <Button className={classes.cancelButton} variant="contained" color="secondary" onClick={onCancel}>
            Cancel
          </Button>
          <Button variant="contained" color="primary" onClick={onSave}>
            Salvar
          </Button>
        </div>
        : null}

      {table}

    </Paper>
  )
}

export default MatriculaPage
